using CliWrap.Buffered;
using ControleFinanceiro.Mcp.Tests.Infraestrutura;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace ControleFinanceiro.Mcp.Tests.Servidor;

[Collection(ServidorMcpCollection.Nome)]
public sealed class ServidorMcpTests
{
    private const string NomeFerramenta = "verificar_servidor";

    private readonly ServidorMcpFixture _fixture;

    public ServidorMcpTests(ServidorMcpFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Inicializar_Deve_IdentificarOServidor()
    {
        _fixture.ClienteMcp.ServerInfo!.Name.Should().Be("ControleFinanceiro.Mcp");
        _fixture.ClienteMcp.ServerCapabilities.Tools.Should().NotBeNull();
    }

    [Fact]
    public async Task ListarFerramentas_Deve_TerAVerificacaoAsLeiturasEOVinculo()
    {
        IList<McpClientTool> ferramentas = await _fixture.ClienteMcp.ListToolsAsync();

        ferramentas
            .Select(ferramenta => ferramenta.Name)
            .Should().BeEquivalentTo(NomeFerramenta, "listar_transacoes", "listar_despesas_fixas", "balanco_mensal", "vincular_transacao");
    }

    [Fact]
    public async Task VerificarServidor_Quando_BancoAcessivel_Deve_ResponderDisponivel()
    {
        string resposta = await VerificarAsync(_fixture.ClienteMcp);

        resposta.Should().Be("""{"banco":"disponivel"}""");
    }

    [Fact]
    public async Task VerificarServidor_Quando_BancoInacessivel_Deve_ResponderIndisponivel()
    {
        await using McpClient clienteMcp = await ServidorMcpFixture.IniciarClienteMcpAsync(_fixture.ConexaoPortaFechada, _fixture.Usuario.Id);

        string resposta = await VerificarAsync(clienteMcp);

        resposta.Should().Be("""{"banco":"indisponivel"}""");
    }

    // O usuário só é lido quando uma consulta chega ao filtro: a verificação não depende dele.
    [Fact]
    public async Task VerificarServidor_Quando_SemUsuarioConfigurado_Deve_ResponderDisponivel()
    {
        await using McpClient clienteMcp = await ServidorMcpFixture.IniciarClienteMcpAsync(_fixture.ConexaoDoContainer, idUsuario: null);

        string resposta = await VerificarAsync(clienteMcp);

        resposta.Should().Be("""{"banco":"disponivel"}""");
    }

    [Fact]
    public async Task Executar_Quando_StdinFecha_Deve_EncerrarSemEscreverNoStdout()
    {
        BufferedCommandResult execucao = await ServidorMcpFixture.ExecutarSemClienteMcpAsync(_fixture.ConexaoDoContainer);

        execucao.ExitCode.Should().Be(0);
        execucao.StandardOutput.Should().BeEmpty();
    }

    [Fact]
    public async Task Executar_Quando_SemConnectionString_Deve_FalharSemEscreverNoStdout()
    {
        BufferedCommandResult execucao = await ServidorMcpFixture.ExecutarSemClienteMcpAsync(conexao: "");

        execucao.ExitCode.Should().NotBe(0);
        execucao.StandardOutput.Should().BeEmpty();
    }

    [Fact]
    public async Task Publicacao_Deve_NaoDependerDeAspNetCore()
    {
        string runtimeConfig = await File.ReadAllTextAsync(Path.Combine(ServidorMcpFixture.PastaServidor, "ControleFinanceiro.Mcp.runtimeconfig.json"));
        string deps = await File.ReadAllTextAsync(Path.Combine(ServidorMcpFixture.PastaServidor, "ControleFinanceiro.Mcp.deps.json"));

        runtimeConfig.Should().NotContain("Microsoft.AspNetCore.App");
        deps.Should().NotContain("ModelContextProtocol.AspNetCore");
    }

    private static async Task<string> VerificarAsync(McpClient clienteMcp)
    {
        CallToolResult resultado = await clienteMcp.CallToolAsync(NomeFerramenta);

        return resultado.Content.OfType<TextContentBlock>().Single().Text;
    }
}
