using ControleFinanceiro.Application;
using ControleFinanceiro.Domain;
using ControleFinanceiro.Domain.Shared;
using System.Reflection;

namespace ControleFinanceiro.SaidaModelo.Tests.Arquitetura;

/// <summary>
/// O sufixo diz a camada: o serviço de domínio termina em <c>Service</c> e o da Application em <c>AppService</c>, ou em
/// <c>ParaModeloService</c> quando fala com o modelo; o que a Application devolve termina em <c>Dto</c>, e o domínio não
/// usa nenhum dos dois sufixos da Application. O evento de domínio termina em <c>Event</c>.
/// </summary>
public sealed class NomesCamadaTests
{
    private static readonly Assembly _application = typeof(ApplicationAssemblyMarker).Assembly;
    private static readonly Assembly _domain = typeof(DomainAssemblyMarker).Assembly;

    [Fact]
    public void ServicosApplication_Deve_TerminarEmAppServiceOuParaModeloService()
    {
        IEnumerable<string> servicos = NomesPublicos(_application).Where(nome => nome.EndsWith("Service", StringComparison.Ordinal));

        servicos.Should().OnlyContain(nome => nome.EndsWith("AppService", StringComparison.Ordinal) || nome.EndsWith("ParaModeloService", StringComparison.Ordinal));
    }

    [Fact]
    public void TiposDomain_Deve_FicarSemOsSufixosApplication()
    {
        NomesPublicos(_domain).Should().NotContain(nome => nome.EndsWith("AppService", StringComparison.Ordinal) || nome.EndsWith("Dto", StringComparison.Ordinal));
    }

    [Fact]
    public void EventosDomain_Deve_TerminarEmEvent()
    {
        IEnumerable<Type> eventos = _domain.GetExportedTypes().Where(tipo => tipo.IsClass && tipo.IsAssignableTo(typeof(IDomainEvent)));

        eventos.Should().NotBeEmpty().And.OnlyContain(tipo => tipo.Name.EndsWith("Event", StringComparison.Ordinal));
    }

    [Fact]
    public void EventosDomain_Deve_DeclararNomesEstaveisDistintos()
    {
        string?[] nomes = _domain.GetExportedTypes()
            .Where(tipo => tipo.IsClass && tipo.IsAssignableTo(typeof(IDomainEvent)))
            .Select(tipo => tipo.GetCustomAttribute<DomainEventNameAttribute>()?.Name)
            .ToArray();

        nomes.Should().NotBeEmpty().And.OnlyContain(nome => !string.IsNullOrWhiteSpace(nome)).And.OnlyHaveUniqueItems();
    }

    private static IEnumerable<string> NomesPublicos(Assembly assembly) =>
        assembly.GetExportedTypes().Where(tipo => !tipo.IsNested).Select(tipo => tipo.Name);
}
