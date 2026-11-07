using System.Text.Json;
using System.Text.Json.Nodes;
using AutoFixture;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Mcp.Tests.Infraestrutura;
using ControleFinanceiro.Tests.Shared.Respostas;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace ControleFinanceiro.Mcp.Tests.Ferramentas;

[Collection(ServidorMcpCollection.Nome)]
public sealed class ListarDespesasFixasToolTests : IAsyncLifetime
{
    private const string NomeFerramenta = "listar_despesas_fixas";

    private readonly ServidorMcpFixture _servidor;
    private readonly BancoDeTeste _banco;

    public ListarDespesasFixasToolTests(ServidorMcpFixture servidor)
    {
        _servidor = servidor;
        _banco = new BancoDeTeste(servidor);
    }

    public Task InitializeAsync() => _servidor.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Esquema_Deve_TerSoOAnoEOMes()
    {
        IList<McpClientTool> ferramentas = await _servidor.ClienteMcp.ListToolsAsync();

        JsonElement esquema = ferramentas.Single(f => f.Name == NomeFerramenta).ProtocolTool.InputSchema;

        esquema
            .GetProperty("properties")
            .EnumerateObject()
            .Select(p => p.Name)
            .Should().BeEquivalentTo("ano", "mes");
    }

    [Theory, DomainAutoData]
    public async Task Listar_Quando_DespesaDoMesNaoGerada_Deve_TrazerARecorrenteSemDespesaDoMes(MesReferencia mes, Fixture fixture)
    {
        fixture.Inject(_servidor.Usuario.Id);
        DespesaRecorrente recorrente = await _banco.GravarRecorrenteAsync(fixture);

        JsonObject json = (await _servidor.ClienteMcp.ChamarDoMesAsync(NomeFerramenta, mes.Ano, mes.Mes)).Json;

        json.Select(campo => campo.Key).Should().BeEquivalentTo("mes", "despesasFixas");
        json["despesasFixas"]![0]!.AsObject()
            .Select(campo => campo.Key)
            .Should().BeEquivalentTo("id", "descricao", "valor", "diaOcorrencia");
        DespesaFixaLida fixa = new DespesasFixasDoMesLidas(json).DespesasFixas.Single();
        fixa.Id.Should().Be(recorrente.Id.Value);
        fixa.Valor.Should().Be(recorrente.Valor);
        fixa.DiaOcorrencia.Should().Be(recorrente.DiaOcorrencia);
    }

    [Theory, DomainAutoData]
    public async Task Listar_Quando_DespesaDoMesGerada_Deve_TrazerADespesaComOStatus(MesReferencia mes, Fixture fixture)
    {
        fixture.Inject(_servidor.Usuario.Id);
        DespesaRecorrente recorrente = await _banco.GravarRecorrenteAsync(fixture);
        Despesa despesa = await _banco.GravarDespesaDoMesAsync(recorrente, mes);

        CallToolResult resultado = await _servidor.ClienteMcp.ChamarDoMesAsync(NomeFerramenta, mes.Ano, mes.Mes);

        resultado.Json["despesasFixas"]![0]!["despesaDoMes"]!.AsObject()
            .Select(campo => campo.Key)
            .Should().BeEquivalentTo("id", "descricao", "dataOcorrencia", "valor", "mes", "status");
        DespesaDoMesLida despesaDoMes = new DespesasFixasDoMesLidas(resultado.Json).DespesasFixas.Single().DespesaDoMes!;
        despesaDoMes.Id.Should().Be(despesa.Id.Value);
        despesaDoMes.Mes.Should().Be(mes.ToString());
        despesaDoMes.Status.Should().Be(StatusPagamento.Pendente.Name);
        resultado.Texto.Should().NotContain(recorrente.IdBeneficiario.Value.ToString())
            .And.NotContain(recorrente.IdCategoria.Value.ToString())
            .And.NotContain(recorrente.IdUsuario.Value.ToString());
    }

    [Theory, DomainAutoData]
    public async Task Listar_Quando_DespesaDeOutroMes_Deve_TrazerARecorrenteSemDespesaDoMes(MesReferencia mes, Fixture fixture)
    {
        fixture.Inject(_servidor.Usuario.Id);
        DespesaRecorrente recorrente = await _banco.GravarRecorrenteAsync(fixture);
        await _banco.GravarDespesaDoMesAsync(recorrente, mes.Posterior());

        DespesasFixasDoMesLidas lidas = await ListarAsync(mes);

        lidas.DespesasFixas.Single().DespesaDoMes.Should().BeNull();
    }

    [Theory]
    [InlineDomainAutoData(FormaDocumento.CpfComPontuacao)]
    [InlineDomainAutoData(FormaDocumento.CnpjPuro)]
    [InlineDomainAutoData(FormaDocumento.CnpjComLetrasComPontuacao)]
    public async Task Listar_Quando_DocumentoNaDescricao_Deve_SairMascaradoNaFixaENaDespesaDoMes(
        FormaDocumento forma,
        DocumentosDeTeste documentos,
        MesReferencia mes,
        Fixture fixture)
    {
        fixture.Inject(_servidor.Usuario.Id);
        fixture.Inject((TextoObrigatorio)$"ALUGUEL {documentos.Em(forma)}");
        DespesaRecorrente recorrente = await _banco.GravarRecorrenteAsync(fixture);
        await _banco.GravarDespesaDoMesAsync(recorrente, mes);

        DespesaFixaLida fixa = (await ListarAsync(mes)).DespesasFixas.Single();

        string esperada = $"ALUGUEL {DocumentosDeTeste.Rotulo(forma)}";
        fixa.Descricao.Should().Be(esperada);
        fixa.DespesaDoMes!.Descricao.Should().Be(esperada);
    }

    [Theory, DomainAutoData]
    public async Task Listar_Quando_RecorrenteDeOutroUsuario_Deve_NaoTrazerA(Usuario outroUsuario, MesReferencia mes, Fixture fixture)
    {
        await _banco.GravarUsuarioAsync(outroUsuario);
        fixture.Inject(outroUsuario.Id);
        await _banco.GravarRecorrenteAsync(fixture);

        DespesasFixasDoMesLidas lidas = await ListarAsync(mes);

        lidas.DespesasFixas.Should().BeEmpty();
    }

    [Fact]
    public async Task Listar_Quando_MesInvalido_Deve_DevolverSoOErroSemRepetirOMes()
    {
        CallToolResult resultado = await _servidor.ClienteMcp.ChamarDoMesAsync(NomeFerramenta, ano: 2026, mes: 13);

        resultado.Json.Select(campo => campo.Key).Should().Equal("erro");
        resultado.Texto.Should().NotContain("13");
    }

    private async Task<DespesasFixasDoMesLidas> ListarAsync(MesReferencia mes)
    {
        CallToolResult resultado = await _servidor.ClienteMcp.ChamarDoMesAsync(NomeFerramenta, mes.Ano, mes.Mes);
        return new DespesasFixasDoMesLidas(resultado.Json);
    }
}
