using AutoFixture;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Mcp.Tests.Infraestrutura;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using ControleFinanceiro.Tests.Shared.Respostas;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace ControleFinanceiro.Mcp.Tests.Ferramentas;

/// <summary>Os cartões pelo processo real, como o modelo os recebe para achar a fatura.</summary>
[Collection(ServidorMcpCollection.Nome)]
public sealed class ListarCartoesToolTests : IAsyncLifetime
{
    private readonly ServidorMcpFixture _servidor;
    private readonly BancoDeTeste _banco;

    public ListarCartoesToolTests(ServidorMcpFixture servidor)
    {
        _servidor = servidor;
        _banco = new BancoDeTeste(servidor);
    }

    public Task InitializeAsync() => _servidor.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Esquema_Deve_NaoTerParametros()
    {
        McpClientTool ferramenta = await _servidor.ClienteMcp.ObterFerramentaAsync("listar_cartoes");

        ferramenta.ProtocolTool.InputSchema.GetProperty("properties").EnumerateObject().Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public async Task Listar_Deve_DevolverOsCartoesDoUsuario(Fixture fixture)
    {
        fixture.Inject(_servidor.Usuario.Id);
        Cartao cartao = fixture.Create<Cartao>();
        await _banco.GravarCartaoAsync(cartao);

        CallToolResult resultado = await _servidor.ClienteMcp.ListarCartoesAsync();

        CartaoLido lido = new CartoesLidos(resultado.Json).Cartoes.Should().ContainSingle().Subject;
        lido.Id.Should().Be(cartao.Id.Value);
        lido.Banco.Should().Be(cartao.Banco.Name);
        lido.Final.Should().Be(cartao.Sufixo);
    }

    [Theory, DomainAutoData]
    public async Task Listar_Quando_CartaoDeOutroUsuario_Deve_NaoDevolver(Usuario outroUsuario, Fixture fixture)
    {
        await _banco.GravarUsuarioAsync(outroUsuario);
        fixture.Inject(outroUsuario.Id);
        await _banco.GravarCartaoAsync(fixture.Create<Cartao>());

        CallToolResult resultado = await _servidor.ClienteMcp.ListarCartoesAsync();

        new CartoesLidos(resultado.Json).Cartoes.Should().BeEmpty();
    }
}
