using System.Text.Json;
using System.Text.Json.Nodes;
using AutoFixture;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Mcp.Tests.Infraestrutura;
using ControleFinanceiro.Mcp.Tests.Infraestrutura.Respostas;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace ControleFinanceiro.Mcp.Tests.Ferramentas;

[Collection(ServidorMcpCollection.Nome)]
public sealed class BalancoMensalFerramentaTests : IAsyncLifetime
{
    private const string NomeFerramenta = "balanco_mensal";

    private readonly ServidorMcpFixture _servidor;
    private readonly BancoDeTeste _banco;

    public BalancoMensalFerramentaTests(ServidorMcpFixture servidor)
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
    public async Task Obter_Deve_TrazerSoOMesEOsTotais(MesReferencia mes, Fixture fixture)
    {
        fixture.Inject(_servidor.Usuario.Id);
        DespesaRecorrente recorrente = await _banco.GravarRecorrenteAsync(fixture);
        await _banco.GravarDespesaDoMesAsync(recorrente, mes);

        JsonObject json = (await _servidor.ClienteMcp.ChamarDoMesAsync(NomeFerramenta, mes.Ano, mes.Mes)).Json;

        json.Select(campo => campo.Key).Should().BeEquivalentTo("mes", "rendaLiquida", "pago", "pendente", "saldo");
        BalancoLido balanco = new(json);
        balanco.Mes.Should().Be(mes.ToString());
        balanco.RendaLiquida.Should().Be(_servidor.Usuario.RendaLiquida);
        balanco.Pago.Should().Be(0m);
        balanco.Pendente.Should().Be(recorrente.Valor);
        balanco.Saldo.Should().Be(_servidor.Usuario.RendaLiquida - recorrente.Valor);
    }

    [Fact]
    public async Task Obter_Deve_NaoTrazerONomeNemOsDescontos()
    {
        CallToolResult resultado = await _servidor.ClienteMcp.ChamarDoMesAsync(NomeFerramenta, ano: 2026, mes: 3);

        resultado.Texto.Should().NotContain(_servidor.Usuario.Nome);
        foreach (DescontoSalarial desconto in _servidor.Usuario.DescontosSalariais)
            resultado.Texto.Should().NotContain(desconto.Descricao);
    }

    [Theory, DomainAutoData]
    public async Task Obter_Quando_DespesaDeOutroUsuario_Deve_NaoContarNoPendente(Usuario outroUsuario, MesReferencia mes, Fixture fixture)
    {
        await _banco.GravarUsuarioAsync(outroUsuario);
        fixture.Inject(outroUsuario.Id);
        DespesaRecorrente recorrente = await _banco.GravarRecorrenteAsync(fixture);
        await _banco.GravarDespesaDoMesAsync(recorrente, mes);

        BalancoLido balanco = new((await _servidor.ClienteMcp.ChamarDoMesAsync(NomeFerramenta, mes.Ano, mes.Mes)).Json);

        balanco.Pendente.Should().Be(0m);
    }

    [Fact]
    public async Task Obter_Quando_MesInvalido_Deve_DevolverSoOErroSemRepetirOMes()
    {
        CallToolResult resultado = await _servidor.ClienteMcp.ChamarDoMesAsync(NomeFerramenta, ano: 2026, mes: 13);

        resultado.Json.Select(campo => campo.Key).Should().Equal("erro");
        resultado.Texto.Should().NotContain("13");
    }

    [Theory, DomainAutoData]
    public async Task Obter_Quando_UsuarioConfiguradoNaoExiste_Deve_DevolverOErro(IdUsuario usuarioInexistente)
    {
        await using McpClient clienteMcp = await ServidorMcpFixture.IniciarClienteMcpAsync(_servidor.ConexaoDoContainer, usuarioInexistente);

        CallToolResult resultado = await clienteMcp.ChamarDoMesAsync(NomeFerramenta, ano: 2026, mes: 3);

        resultado.Json.Select(campo => campo.Key).Should().Equal("erro");
        resultado.Texto.Should().NotContain(usuarioInexistente.Value.ToString());
    }
}
