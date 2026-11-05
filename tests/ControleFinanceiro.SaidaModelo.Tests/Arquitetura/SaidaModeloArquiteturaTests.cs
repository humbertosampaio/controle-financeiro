using ArchUnitNET.xUnit;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Reflection;
using System.Xml.Linq;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using Architecture = ArchUnitNET.Domain.Architecture;
using ArchLoader = ArchUnitNET.Loader.ArchLoader;
using DependencyInjectionMcp = ControleFinanceiro.Mcp.DependencyInjection;
using DependencyInjectionPersistence = ControleFinanceiro.Infrastructure.Persistence.DependencyInjection;
using IdBeneficiario = ControleFinanceiro.Domain.Beneficiarios.IdBeneficiario;
using DiaOcorrenciaRecorrente = ControleFinanceiro.Domain.Despesas.DiaOcorrenciaRecorrente;
using IdDespesa = ControleFinanceiro.Domain.Despesas.IdDespesa;
using IdDespesaRecorrente = ControleFinanceiro.Domain.Despesas.IdDespesaRecorrente;
using StatusPagamento = ControleFinanceiro.Domain.Despesas.StatusPagamento;
using IdTransacao = ControleFinanceiro.Domain.Transacoes.IdTransacao;

namespace ControleFinanceiro.SaidaModelo.Tests.Arquitetura;

/// <summary>
/// Impede quem fala com o modelo de contornar a fronteira de saída. Escapam destas regras, e quem os cobre é o
/// <c>revisor-privacidade</c>: reflexão, <c>dynamic</c>, ferramenta registrada por delegate, handler registrado pelo builder
/// e os campos que um tipo de saída expõe, conferidos no teste de payload dele.
/// </summary>
public sealed class SaidaModeloArquiteturaTests
{
    private static readonly Assembly _mcp = typeof(DependencyInjectionMcp).Assembly;
    private static readonly Assembly _application = typeof(ISaidaModelo).Assembly;
    private static readonly Assembly _domain = typeof(IdTransacao).Assembly;
    private static readonly Assembly _persistence = typeof(ControleFinanceiroDbContext).Assembly;
    private static readonly Assembly _efCore = typeof(DbContext).Assembly;
    private static readonly Assembly _efRelational = typeof(RelationalDatabaseFacadeExtensions).Assembly;
    private static readonly Assembly _sdkMcp = typeof(McpServer).Assembly;

    private static readonly Architecture _arquitetura = new ArchLoader()
        .LoadAssemblies(_mcp, _application, _domain, _persistence, _efCore, _efRelational, _sdkMcp)
        .Build();

    private const string NomeVerificacaoServidor = "ControleFinanceiro.Mcp.Ferramentas.VerificacaoServidor";

    /// <summary>Os projetos de <c>src/</c> que falam com o modelo: as regras desta classe valem para eles.</summary>
    private static readonly Assembly[] _falamComOModelo = [_mcp];

    private static readonly string[] _naoFalamComOModelo =
    [
        "ControleFinanceiro.AppHost",
        "ControleFinanceiro.Application",
        "ControleFinanceiro.Domain",
        "ControleFinanceiro.Infrastructure.Importacao",
        "ControleFinanceiro.Infrastructure.Persistence",
        "ControleFinanceiro.ServiceDefaults",
        "ControleFinanceiro.WebApi"
    ];

    [Theory]
    [InlineData("ControleFinanceiro.WebApi")]
    [InlineData("ControleFinanceiro.Infrastructure.Importacao")]
    [InlineData("Npgsql")]
    public void QuemFalaComOModelo_Deve_NaoReferenciarOAssembly(string assembly)
    {
        _falamComOModelo
            .SelectMany(projeto => projeto.GetReferencedAssemblies())
            .Select(referencia => referencia.Name)
            .Should().NotContain(assembly);
    }

    [Fact]
    public void QuemFalaComOModelo_Deve_UsarDoDomainSoIdsVogen()
    {
        Type[] proibidos = TiposDe(_domain).Where(tipo => !EhIdVogen(tipo)).ToArray();

        TiposDeQuemFala().Should().NotDependOnAny(proibidos).Check(_arquitetura);
    }

    [Fact]
    public void QuemFalaComOModelo_Deve_UsarDaPersistenceSoODbContextEORegistro()
    {
        Type[] proibidos = TiposDe(_persistence)
            .Where(tipo => tipo != typeof(ControleFinanceiroDbContext) && tipo != typeof(DependencyInjectionPersistence))
            .ToArray();

        TiposDeQuemFala().Should().NotDependOnAny(proibidos).Check(_arquitetura);
    }

    /// <summary>Bloqueia consulta direta ao banco, como <c>ExecuteSql</c> e <c>SqlQuery</c>, que devolveria dado sem passar pelos tipos de saída.</summary>
    [Fact]
    public void QuemFalaComOModelo_Deve_UsarDoEfSoODbContextEODatabaseFacade()
    {
        Type[] proibidos = TiposDe(_efCore)
            .Concat(TiposDe(_efRelational))
            .Where(tipo => tipo != typeof(DbContext) && tipo != typeof(DatabaseFacade))
            .ToArray();

        TiposDeQuemFala().Should().NotDependOnAny(proibidos).Check(_arquitetura);
    }

    /// <summary>A verificação do servidor só pergunta se o banco responde; as demais ferramentas passam pelos tipos de saída.</summary>
    [Fact]
    public void QuemFalaComOModelo_Deve_UsarODbContextSoNaVerificacaoDoServidor()
    {
        TiposDeQuemFala()
            .And().DoNotHaveFullName("ControleFinanceiro.Mcp.Ferramentas.VerificarServidorTool")
            .And().DoNotHaveFullName("ControleFinanceiro.Mcp.Ferramentas.VerificarServidorTool+<Verificar>d__0")
            .Should().NotDependOnAny(typeof(ControleFinanceiroDbContext))
            .Check(_arquitetura);
    }

    [Fact]
    public void TiposDeSaida_Deve_SerClassesSeladas()
    {
        TiposDeSaida().Where(tipo => !tipo.IsClass || !tipo.IsSealed).Should().BeEmpty();
    }

    [Fact]
    public void TiposDeSaida_Deve_NaoTerConstrutorPublico()
    {
        TiposDeSaida().Where(tipo => tipo.GetConstructors().Length > 0).Should().BeEmpty();
    }

    [Fact]
    public void TiposDeSaida_Deve_NaoTerSetter()
    {
        TiposDeSaida()
            .SelectMany(tipo => tipo.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(propriedade => propriedade.GetSetMethod(nonPublic: true) is not null)
            .Should().BeEmpty();
    }

    [Fact]
    public void TiposDeSaida_Deve_TerSoCamposDePropriedade()
    {
        TiposDeSaida()
            .SelectMany(tipo => tipo.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(campo => !campo.Name.EndsWith("k__BackingField", StringComparison.Ordinal))
            .Should().BeEmpty();
    }

    /// <summary>
    /// Texto só entra mascarado: <c>string</c> ou um agregado no construtor abririam a fronteira. Outro tipo de saída, sozinho
    /// ou numa lista, entra porque passa pelas mesmas regras.
    /// </summary>
    [Fact]
    public void TiposDeSaida_Deve_AceitarNoConstrutorSoTiposPermitidos()
    {
        IEnumerable<Type> parametros = TiposDeSaida()
            .SelectMany(tipo => tipo
                .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .SelectMany(construtor => construtor.GetParameters())
                .Select(parametro => parametro.ParameterType)
                .Where(parametro => parametro != tipo)); // O construtor de cópia que o compilador gera para o record.

        parametros.Where(parametro => !EhPermitidoNoConstrutor(parametro)).Should().BeEmpty();
    }

    [Fact]
    public void Ferramentas_Deve_DevolverSoTiposDeSaida()
    {
        IEnumerable<MethodInfo> ferramentas = MetodosDeQuemFala()
            .Where(metodo => metodo.GetCustomAttribute<McpServerToolAttribute>() is not null);

        ferramentas.Should().NotBeEmpty();
        ferramentas
            .Where(ferramenta => !DevolveSaida(ferramenta.ReturnType))
            .Select(ferramenta => ferramenta.Name)
            .Should().BeEmpty();
    }

    /// <summary>A exceção à regra de retorno só pode dizer o estado do banco: um campo novo nela passaria sem as regras de forma.</summary>
    [Fact]
    public void VerificacaoServidor_Deve_TerSoOEstadoDoBanco()
    {
        _mcp.GetType(NomeVerificacaoServidor)!
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(propriedade => propriedade.Name)
            .Should().Equal("Banco");
    }

    /// <summary>Mensagem de exceção, progresso, logging e sampling levam texto ao cliente sem passar pelos tipos de saída.</summary>
    [Fact]
    public void QuemFalaComOModelo_Deve_NaoUsarOutrosCanaisDoSdk()
    {
        Type[] proibidos = TiposDe(_sdkMcp)
            .Where(tipo => tipo.IsAssignableTo(typeof(Exception))
                || tipo.IsAssignableTo(typeof(McpSession))
                || tipo == typeof(RequestContext<>)
                || tipo == typeof(ProgressNotificationValue))
            .ToArray();

        TiposDeQuemFala().Should().NotDependOnAny(proibidos).Check(_arquitetura);
    }

    /// <summary>Prompts e resources também mandam conteúdo ao modelo, e o retorno deles não passa pela fronteira.</summary>
    [Fact]
    public void QuemFalaComOModelo_Deve_NaoTerPromptNemResource()
    {
        MetodosDeQuemFala()
            .Where(metodo => metodo.GetCustomAttribute<McpServerPromptAttribute>() is not null
                || metodo.GetCustomAttribute<McpServerResourceAttribute>() is not null)
            .Select(metodo => metodo.Name)
            .Should().BeEmpty();
    }

    /// <summary>Um projeto novo, como o do agente, quebra este teste até dizer se fala com o modelo.</summary>
    [Fact]
    public void Solucao_Deve_TerCadaProjetoDeSrcClassificado()
    {
        IEnumerable<string> projetos = XDocument.Load(CaminhoDaSolucao())
            .Descendants("Project")
            .Select(projeto => projeto.Attribute("Path")!.Value)
            .Where(caminho => caminho.StartsWith("src/", StringComparison.Ordinal))
            .Select(Path.GetFileNameWithoutExtension)!;

        IEnumerable<string?> classificados = _falamComOModelo
            .Select(projeto => projeto.GetName().Name)
            .Concat(_naoFalamComOModelo);

        projetos.Should().BeEquivalentTo(classificados);
    }

    private static ArchUnitNET.Fluent.Syntax.Elements.Types.GivenTypesConjunction TiposDeQuemFala() =>
        Types().That().ResideInAssembly(_falamComOModelo[0], _falamComOModelo[1..]);

    private static IEnumerable<MethodInfo> MetodosDeQuemFala()
    {
        return _falamComOModelo
            .SelectMany(projeto => projeto.GetTypes())
            .SelectMany(tipo => tipo.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
    }

    /// <summary>Sem os tipos que o compilador gera, que o ArchUnitNET não carrega.</summary>
    private static IEnumerable<Type> TiposDe(Assembly assembly) => assembly.GetTypes().Where(tipo => tipo.FullName?.Contains('<') == false);

    private static IEnumerable<Type> TiposDeSaida()
    {
        return _falamComOModelo
            .Append(_application)
            .SelectMany(projeto => projeto.GetTypes())
            .Where(tipo => !tipo.IsInterface && tipo.IsAssignableTo(typeof(ISaidaModelo)));
    }

    private static bool EhIdVogen(Type tipo) => tipo.IsValueType && !tipo.IsEnum && tipo.Name.StartsWith("Id", StringComparison.Ordinal);

    private static bool EhPermitidoNoConstrutor(Type tipo)
    {
        return tipo == typeof(DocumentoMascarado)
            || tipo == typeof(IReadOnlyList<DocumentoMascarado>)
            || tipo == typeof(DateOnly)
            || tipo == typeof(Reais)
            || tipo == typeof(MesReferencia)
            || tipo == typeof(StatusPagamento)
            || tipo == typeof(DiaOcorrenciaRecorrente)
            || tipo == typeof(IdTransacao)
            || tipo == typeof(IdDespesa)
            || tipo == typeof(IdDespesaRecorrente)
            || tipo == typeof(IdBeneficiario)
            || EhSaida(tipo)
            || EhListaDeSaida(tipo);
    }

    private static bool EhSaida(Type tipo) => !tipo.IsInterface && tipo.IsAssignableTo(typeof(ISaidaModelo));

    private static bool EhListaDeSaida(Type tipo)
    {
        return tipo.IsGenericType
            && tipo.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)
            && EhSaida(tipo.GetGenericArguments()[0]);
    }

    /// <summary>
    /// Olha dentro de <c>Task</c>, <c>ValueTask</c> e coleções, em todos os argumentos genéricos. A verificação do servidor
    /// é interna, e o repositório não usa <c>InternalsVisibleTo</c>: por isso, o nome completo.
    /// </summary>
    private static bool DevolveSaida(Type tipo)
    {
        if (tipo.IsAssignableTo(typeof(ISaidaModelo)) || tipo.FullName == NomeVerificacaoServidor)
            return true;

        return EhEmbrulho(tipo) && tipo.GetGenericArguments().All(DevolveSaida);
    }

    private static bool EhEmbrulho(Type tipo)
    {
        if (!tipo.IsGenericType)
            return false;

        Type definicao = tipo.GetGenericTypeDefinition();
        return definicao == typeof(Task<>)
            || definicao == typeof(ValueTask<>)
            || tipo
                .GetInterfaces()
                .Append(tipo)
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
    }

    private static string CaminhoDaSolucao()
    {
        DirectoryInfo? pasta = new(AppContext.BaseDirectory);
        while (pasta is not null && !File.Exists(Path.Combine(pasta.FullName, "ControleFinanceiro.slnx")))
            pasta = pasta.Parent;

        return Path.Combine(pasta!.FullName, "ControleFinanceiro.slnx");
    }
}
