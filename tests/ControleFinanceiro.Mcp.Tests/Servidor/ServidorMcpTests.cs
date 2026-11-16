using CliWrap.Buffered;
using ControleFinanceiro.Application.IA;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Mcp.Tests.Infraestrutura;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text.Json;

namespace ControleFinanceiro.Mcp.Tests.Servidor;

[Collection(ServidorMcpCollection.Nome)]
public sealed class ServidorMcpTests
{
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
    public async Task ListarFerramentas_Deve_TerAVerificacaoAsLeiturasEAsDuasEtapasDoVinculoDoPagamentoDaFaturaEDaImportacao()
    {
        IList<McpClientTool> ferramentas = await _fixture.ClienteMcp.ListToolsAsync();

        ferramentas
            .Select(ferramenta => ferramenta.Name)
            .Should().BeEquivalentTo(
                "verificar_servidor",

                "listar_transacoes",
                "listar_despesas_fixas",
                "balanco_mensal",

                "prever_vinculo",
                "confirmar_vinculo",

                "listar_cartoes",
                "listar_candidatos_pagamento_fatura",
                "prever_pagamento_fatura",
                "confirmar_pagamento_fatura",

                "prever_importacao_c6_conta_csv",
                "confirmar_importacao_c6_conta_csv",
                "prever_importacao_mercado_pago_conta_csv",
                "confirmar_importacao_mercado_pago_conta_csv",
                "prever_importacao_c6_cartao_csv",
                "confirmar_importacao_c6_cartao_csv",
                "prever_importacao_mercado_pago_cartao_csv",
                "confirmar_importacao_mercado_pago_cartao_csv");
    }

    // Os nomes saem do código do layout, e não da Tools: um layout novo sem as suas ferramentas quebra este teste.
    [Fact]
    public async Task ListarFerramentas_Deve_TerUmaPreviaEUmaConfirmacaoPorLayout()
    {
        IList<McpClientTool> ferramentas = await _fixture.ClienteMcp.ListToolsAsync();

        IEnumerable<string> esperadas = LayoutArquivoImportacao.List
            .Select(layout => layout.Value.Replace('-', '_'))
            .SelectMany(codigo => new[] { $"prever_importacao_{codigo}", $"confirmar_importacao_{codigo}" });
        ferramentas.Select(ferramenta => ferramenta.Name).Should().Contain(esperadas);
    }

    // O cliente descarta o tipo em lista, como o ["string", "null"] do parâmetro anulável, e sem o tipo o modelo
    // escolhe o formato do argumento, que o SDK pode não converter.
    [Fact]
    public async Task ListarFerramentas_Deve_PublicarUmTipoSoEmCadaParametro()
    {
        IList<McpClientTool> ferramentas = await _fixture.ClienteMcp.ListToolsAsync();

        IEnumerable<string> semTipoUnico = ferramentas
            .SelectMany(ferramenta => ferramenta.ProtocolTool.InputSchema
                .GetProperty("properties")
                .EnumerateObject()
                .Where(parametro => !parametro.Value.TryGetProperty("type", out JsonElement tipo) || tipo.ValueKind != JsonValueKind.String)
                .Select(parametro => $"{ferramenta.Name}.{parametro.Name}"));
        semTipoUnico.Should().BeEmpty();
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

    // O SDK responde ao modelo só que houve um erro: a exceção, com o tipo e a pilha, fica no log do servidor.
    [Fact]
    public async Task ChamarFerramenta_Quando_LancaExcecao_Deve_GravarOTipoEAPilhaNoLog()
    {
        string caminhoLog = ServidorMcpFixture.NovoCaminhoLog();
        string resposta;

        // Descartar o cliente fecha o stdin, e o servidor esvazia o log ao sair.
        await using (McpClient clienteMcp = await ServidorMcpFixture.IniciarClienteMcpAsync(_fixture.ConexaoDoContainer, idUsuario: null, caminhoLog))
        {
            CallToolResult resultado = await clienteMcp.CallToolAsync(Tools.ListarTransacoes, new Dictionary<string, object?> { ["ano"] = 2026, ["mes"] = 3 });
            resultado.IsError.Should().BeTrue();
            resposta = resultado.Content.OfType<TextContentBlock>().Single().Text;
        }

        LerLog(Path.GetDirectoryName(caminhoLog)!).Should().Contain($"\"{Tools.ListarTransacoes}\" threw an unhandled exception")
            .And.Contain("System.InvalidOperationException")
            .And.Contain("UsuarioAtualConfigurado")
            .And.Contain("   at ");
        resposta.Should().NotContain("InvalidOperationException");
    }

    // No Windows, com o repositório e o temporário em discos diferentes, o caminho relativo entre eles não existe: o
    // GetRelativePath devolve o absoluto, e o teste passa sem exercitar a âncora. No CI, em Linux, ele a exercita.
    [Fact]
    public async Task Executar_Quando_CaminhoDoLogRelativo_Deve_GravarAPartirDaPastaDoServidor()
    {
        string diretorioTrabalho = Directory.CreateDirectory(Path.Combine(ServidorMcpFixture.RepositorioDeTeste, Guid.NewGuid().ToString())).FullName;
        string pastaDestino = Path.GetDirectoryName(ServidorMcpFixture.NovoCaminhoLog())!;
        string caminhoRelativo = Path.Combine(Path.GetRelativePath(ServidorMcpFixture.PastaServidor, pastaDestino), "mcp-.log");

        await ServidorMcpFixture.ExecutarSemClienteMcpAsync(_fixture.ConexaoDoContainer, caminhoRelativo, diretorioTrabalho);

        LerLog(pastaDestino).Should().Contain("Application started");
        Directory.EnumerateFileSystemEntries(diretorioTrabalho).Should().BeEmpty();
    }

    [Fact]
    public async Task Publicacao_Deve_NaoDependerDeAspNetCore()
    {
        string runtimeConfig = await File.ReadAllTextAsync(Path.Combine(ServidorMcpFixture.PastaServidor, "ControleFinanceiro.Mcp.runtimeconfig.json"));
        string deps = await File.ReadAllTextAsync(Path.Combine(ServidorMcpFixture.PastaServidor, "ControleFinanceiro.Mcp.deps.json"));

        runtimeConfig.Should().NotContain("Microsoft.AspNetCore.App");
        deps.Should().NotContain("ModelContextProtocol.AspNetCore");
    }

    /// <summary>O texto de todos os arquivos da pasta, que o sink pode dividir por dia ou por tamanho.</summary>
    private static string LerLog(string pasta) =>
        string.Concat(Directory.EnumerateFiles(pasta).Order().Select(File.ReadAllText));

    private static async Task<string> VerificarAsync(McpClient clienteMcp)
    {
        CallToolResult resultado = await clienteMcp.CallToolAsync(Tools.VerificarServidor);

        return resultado.Content.OfType<TextContentBlock>().Single().Text;
    }
}
