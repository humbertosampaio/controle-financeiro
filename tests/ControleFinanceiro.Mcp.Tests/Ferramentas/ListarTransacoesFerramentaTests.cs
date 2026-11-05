using System.Text.Json;
using System.Text.Json.Nodes;
using AutoFixture;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Mcp.Tests.Infraestrutura;
using ControleFinanceiro.Mcp.Tests.Infraestrutura.Respostas;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace ControleFinanceiro.Mcp.Tests.Ferramentas;

[Collection(ServidorMcpCollection.Nome)]
public sealed class ListarTransacoesFerramentaTests : IAsyncLifetime
{
    private const string NomeFerramenta = "listar_transacoes";

    private readonly ServidorMcpFixture _servidor;
    private readonly BancoDeTeste _banco;

    public ListarTransacoesFerramentaTests(ServidorMcpFixture servidor)
    {
        _servidor = servidor;
        _banco = new BancoDeTeste(servidor);
    }

    public Task InitializeAsync() => _servidor.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // Um serviço sem registro apareceria aqui como parâmetro que o modelo teria de preencher.
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
    public async Task Listar_Deve_TrazerSoAsTransacoesDoMesDoPrimeiroAoUltimoDia(MesReferencia mes, Fixture fixture)
    {
        fixture.Inject(_servidor.Usuario.Id);
        IReadOnlyList<Transacao> transacoes = await GravarExtratoAsync(
            fixture,
            NaData(fixture, mes.Inicio),
            NaData(fixture, mes.Fim),
            NaData(fixture, mes.Fim.AddDays(1)));

        TransacoesDoMesLidas lidas = await ListarAsync(mes);

        lidas.Mes.Should().Be(mes.ToString());
        lidas.Transacoes.Select(t => t.Id).Should().Equal(transacoes[0].Id.Value, transacoes[1].Id.Value);
    }

    [Theory, DomainAutoData]
    public async Task Listar_Deve_TerSoOsCamposPermitidos(MesReferencia mes, Fixture fixture)
    {
        fixture.Inject(_servidor.Usuario.Id);
        IReadOnlyList<Transacao> transacoes = await GravarExtratoAsync(fixture, NaData(fixture, mes.Inicio));
        Transacao transacao = transacoes.Single();

        CallToolResult resultado = await _servidor.ClienteMcp.ChamarDoMesAsync(NomeFerramenta, mes.Ano, mes.Mes);

        resultado.Json.Select(campo => campo.Key).Should().BeEquivalentTo("mes", "transacoes");
        resultado.Json["transacoes"]![0]!.AsObject()
            .Select(campo => campo.Key)
            .Should().BeEquivalentTo("id", "data", "valor", "descricao", "contraparte");
        resultado.Texto.Should().NotContain(transacao.Chave)
            .And.NotContain(transacao.IdUsuario.Value.ToString())
            .And.NotContain(transacao.IdImportacao.Value.ToString())
            .And.NotContain(transacao.Origem.IdConta!.Value.Value.ToString());
    }

    [Theory]
    [InlineDomainAutoData(FormaDocumento.CpfComPontuacao)]
    [InlineDomainAutoData(FormaDocumento.CnpjPuro)]
    [InlineDomainAutoData(FormaDocumento.CnpjComLetrasComPontuacao)]
    public async Task Listar_Quando_DocumentoNaDescricaoENaContraparte_Deve_SairMascarado(
        FormaDocumento forma,
        DocumentosDeTeste documentos,
        MesReferencia mes,
        Fixture fixture)
    {
        fixture.Inject(_servidor.Usuario.Id);
        Lancamento comDocumentos = NaData(fixture, mes.Inicio)
            .ComTextos((TextoObrigatorio)$"PIX {documentos.Em(forma)}", $"FULANO {documentos.Em(forma)}");
        await GravarExtratoAsync(fixture, comDocumentos);

        TransacaoLida transacao = (await ListarAsync(mes)).Transacoes.Single();

        transacao.Descricao.Should().Be($"PIX {DocumentosDeTeste.Rotulo(forma)}");
        transacao.Contraparte.Should().Be($"FULANO {DocumentosDeTeste.Rotulo(forma)}");
    }

    [Theory, DomainAutoData]
    public async Task Listar_Quando_ContraparteAusente_Deve_OmitirOCampo(MesReferencia mes, Fixture fixture)
    {
        fixture.Inject(_servidor.Usuario.Id);
        Lancamento lancamento = NaData(fixture, mes.Inicio);
        await GravarExtratoAsync(fixture, lancamento.ComTextos(lancamento.Descricao, contraparte: null));

        JsonObject json = (await _servidor.ClienteMcp.ChamarDoMesAsync(NomeFerramenta, mes.Ano, mes.Mes)).Json;

        json["transacoes"]![0]!.AsObject().Should().NotContainKey("contraparte");
    }

    [Theory, DomainAutoData]
    public async Task Listar_Quando_TransacaoDeOutroUsuario_Deve_NaoTrazerA(Usuario outroUsuario, MesReferencia mes, Fixture fixture)
    {
        await _banco.GravarUsuarioAsync(outroUsuario);
        fixture.Inject(outroUsuario.Id);
        await GravarExtratoAsync(fixture, NaData(fixture, mes.Inicio));

        TransacoesDoMesLidas lidas = await ListarAsync(mes);

        lidas.Transacoes.Should().BeEmpty();
    }

    [Fact]
    public async Task Listar_Quando_MesInvalido_Deve_DevolverSoOErroSemRepetirOMes()
    {
        CallToolResult resultado = await _servidor.ClienteMcp.ChamarDoMesAsync(NomeFerramenta, ano: 2026, mes: 13);

        resultado.IsError.Should().NotBe(true);
        resultado.Json.Select(campo => campo.Key).Should().Equal("erro");
        resultado.Texto.Should().NotContain("13");
    }

    [Fact]
    public async Task Listar_Quando_SemUsuarioConfigurado_Deve_ResponderErroGenerico()
    {
        await using McpClient clienteMcp = await ServidorMcpFixture.IniciarClienteMcpAsync(_servidor.ConexaoDoContainer, idUsuario: null);

        CallToolResult resultado = await clienteMcp.ChamarDoMesAsync(NomeFerramenta, ano: 2026, mes: 3);

        resultado.IsError.Should().BeTrue();
        resultado.Texto.Should().NotContain("Mcp:IdUsuario")
            .And.NotContain(_servidor.Usuario.Id.Value.ToString())
            .And.NotContain(new Npgsql.NpgsqlConnectionStringBuilder(_servidor.ConexaoDoContainer).Password!);
    }

    private async Task<TransacoesDoMesLidas> ListarAsync(MesReferencia mes)
    {
        CallToolResult resultado = await _servidor.ClienteMcp.ChamarDoMesAsync(NomeFerramenta, mes.Ano, mes.Mes);
        return new TransacoesDoMesLidas(resultado.Json);
    }

    private static Lancamento NaData(IFixture fixture, DateOnly data)
    {
        fixture.Inject(data);
        return fixture.Create<Lancamento>().SemCategoria();
    }

    /// <summary>Grava os lançamentos como um extrato de uma conta nova do usuário injetado no <paramref name="fixture"/>.</summary>
    private Task<IReadOnlyList<Transacao>> GravarExtratoAsync(IFixture fixture, params Lancamento[] lancamentos)
    {
        ContaBancaria conta = fixture.Freeze<ContaBancaria>();

        // O Postgres só grava o instante em UTC, como o TimeProvider da aplicação o dá.
        fixture.Inject(fixture.Create<DateTimeOffset>().ToUniversalTime());

        return _banco.GravarExtratoAsync(conta, fixture.Create<ImportacaoArquivo>(), lancamentos);
    }
}
