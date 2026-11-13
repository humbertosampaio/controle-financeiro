using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Infrastructure.Importacao.Arquivos;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Infrastructure.Importacao.Tests.Arquivos;

/// <summary>
/// Cada teste tem a sua pasta temporária, com a raiz da origem em <c>&lt;tmp&gt;/&lt;guid&gt;/dados</c>: a pasta do
/// guid faz o papel da raiz do repositório, de onde parte o caminho relativo.
/// </summary>
public sealed class OrigemArquivoImportacaoLocalTests : IDisposable
{
    private const string Conteudo = "conteudo-ficticio";

    private readonly string _repositorio;
    private readonly string _raiz;
    private readonly OrigemArquivoImportacaoLocal _origem;

    public OrigemArquivoImportacaoLocalTests()
    {
        _repositorio = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _raiz = Path.Combine(_repositorio, "dados");
        Directory.CreateDirectory(_raiz);
        _origem = new OrigemArquivoImportacaoLocal(_raiz);
    }

    public void Dispose()
    {
        Directory.Delete(_repositorio, recursive: true);
    }

    [Fact]
    public async Task AbrirAsync_Quando_CaminhoRelativoARaizDoRepositorio_Deve_AbrirOArquivo()
    {
        CriarArquivo("dados/2026/extrato.csv");

        Result<Stream, FalhaOrigemArquivoImportacao> result = await _origem.AbrirAsync("dados/2026/extrato.csv", CancellationToken.None);

        (await LerAsync(result)).Should().Be(Conteudo);
    }

    [Fact]
    public async Task AbrirAsync_Quando_CaminhoAbsolutoDentroDaOrigem_Deve_AbrirOArquivo()
    {
        string arquivo = CriarArquivo("dados/extrato.csv");

        Result<Stream, FalhaOrigemArquivoImportacao> result = await _origem.AbrirAsync(arquivo, CancellationToken.None);

        (await LerAsync(result)).Should().Be(Conteudo);
    }

    [Fact]
    public async Task AbrirAsync_Quando_PontoPontoSaiDaOrigem_Deve_RetornarForaDaOrigem()
    {
        CriarArquivo("fora.csv");

        Result<Stream, FalhaOrigemArquivoImportacao> result = await _origem.AbrirAsync("dados/../fora.csv", CancellationToken.None);

        result.Should().FailWith(FalhaOrigemArquivoImportacao.ForaDaOrigem);
    }

    [Fact]
    public async Task AbrirAsync_Quando_CaminhoAbsolutoForaDaOrigem_Deve_RetornarForaDaOrigem()
    {
        string arquivo = CriarArquivo("outra/extrato.csv");

        Result<Stream, FalhaOrigemArquivoImportacao> result = await _origem.AbrirAsync(arquivo, CancellationToken.None);

        result.Should().FailWith(FalhaOrigemArquivoImportacao.ForaDaOrigem);
    }

    [Fact]
    public async Task AbrirAsync_Quando_PastaIrmaComOMesmoPrefixo_Deve_RetornarForaDaOrigem()
    {
        CriarArquivo("dados-outro/extrato.csv");

        Result<Stream, FalhaOrigemArquivoImportacao> result = await _origem.AbrirAsync("dados-outro/extrato.csv", CancellationToken.None);

        result.Should().FailWith(FalhaOrigemArquivoImportacao.ForaDaOrigem);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("dados/extr\0ato.csv")]
    public async Task AbrirAsync_Quando_CaminhoVazioOuComCaractereInvalido_Deve_RetornarForaDaOrigem(string caminho)
    {
        CriarArquivo("dados/extrato.csv");

        Result<Stream, FalhaOrigemArquivoImportacao> result = await _origem.AbrirAsync(caminho, CancellationToken.None);

        result.Should().FailWith(FalhaOrigemArquivoImportacao.ForaDaOrigem);
    }

    [FactSoNoWindows]
    public async Task AbrirAsync_Quando_CaixaDiferenteNoWindows_Deve_AbrirOArquivo()
    {
        CriarArquivo("dados/extrato.csv");

        Result<Stream, FalhaOrigemArquivoImportacao> result = await _origem.AbrirAsync("DADOS/EXTRATO.csv", CancellationToken.None);

        (await LerAsync(result)).Should().Be(Conteudo);
    }

    [Fact]
    public async Task AbrirAsync_Quando_ArquivoEhLinkParaFora_Deve_RetornarForaDaOrigem()
    {
        string fora = CriarArquivo("fora.csv");
        File.CreateSymbolicLink(Path.Combine(_raiz, "link.csv"), fora);

        Result<Stream, FalhaOrigemArquivoImportacao> result = await _origem.AbrirAsync("dados/link.csv", CancellationToken.None);

        result.Should().FailWith(FalhaOrigemArquivoImportacao.ForaDaOrigem);
    }

    [Fact]
    public async Task AbrirAsync_Quando_PastaEhLinkParaFora_Deve_RetornarForaDaOrigem()
    {
        CriarArquivo("outra/extrato.csv");
        Directory.CreateSymbolicLink(Path.Combine(_raiz, "link"), Path.Combine(_repositorio, "outra"));

        Result<Stream, FalhaOrigemArquivoImportacao> result = await _origem.AbrirAsync("dados/link/extrato.csv", CancellationToken.None);

        result.Should().FailWith(FalhaOrigemArquivoImportacao.ForaDaOrigem);
    }

    [Fact]
    public async Task AbrirAsync_Quando_RaizEhLink_Deve_AbrirOArquivoDoAlvo()
    {
        CriarArquivo("dados/extrato.csv");
        string link = Path.Combine(_repositorio, "atalho");
        Directory.CreateSymbolicLink(link, _raiz);
        OrigemArquivoImportacaoLocal origem = new(link);

        Result<Stream, FalhaOrigemArquivoImportacao> result = await origem.AbrirAsync("atalho/extrato.csv", CancellationToken.None);

        (await LerAsync(result)).Should().Be(Conteudo);
    }

    [Fact]
    public async Task AbrirAsync_Quando_ExtensaoEmMaiusculas_Deve_AbrirOArquivo()
    {
        CriarArquivo("dados/extrato.CSV");

        Result<Stream, FalhaOrigemArquivoImportacao> result = await _origem.AbrirAsync("dados/extrato.CSV", CancellationToken.None);

        (await LerAsync(result)).Should().Be(Conteudo);
    }

    [Fact]
    public async Task AbrirAsync_Quando_ExtensaoNaoEhCsv_Deve_RetornarExtensaoNaoAceita()
    {
        CriarArquivo("dados/extrato.txt");

        Result<Stream, FalhaOrigemArquivoImportacao> result = await _origem.AbrirAsync("dados/extrato.txt", CancellationToken.None);

        result.Should().FailWith(FalhaOrigemArquivoImportacao.ExtensaoNaoAceita);
    }

    [Fact]
    public async Task AbrirAsync_Quando_ArquivoNaoExiste_Deve_RetornarNaoEncontrado()
    {
        Result<Stream, FalhaOrigemArquivoImportacao> result = await _origem.AbrirAsync("dados/extrato.csv", CancellationToken.None);

        result.Should().FailWith(FalhaOrigemArquivoImportacao.NaoEncontrado);
    }

    [Fact]
    public async Task AbrirAsync_Quando_DiretorioNoLugarDoArquivo_Deve_RetornarNaoEncontrado()
    {
        Directory.CreateDirectory(Path.Combine(_raiz, "extrato.csv"));

        Result<Stream, FalhaOrigemArquivoImportacao> result = await _origem.AbrirAsync("dados/extrato.csv", CancellationToken.None);

        result.Should().FailWith(FalhaOrigemArquivoImportacao.NaoEncontrado);
    }

    [Fact]
    public async Task AbrirAsync_Quando_ArquivoNoTamanhoMaximo_Deve_AbrirOArquivo()
    {
        CriarArquivoComTamanho("dados/extrato.csv", OrigemArquivoImportacaoLocal.TamanhoMaximoArquivo);

        Result<Stream, FalhaOrigemArquivoImportacao> result = await _origem.AbrirAsync("dados/extrato.csv", CancellationToken.None);

        await using Stream arquivo = result.Should().Succeed().Which;
        arquivo.Length.Should().Be(OrigemArquivoImportacaoLocal.TamanhoMaximoArquivo);
    }

    [Fact]
    public async Task AbrirAsync_Quando_ArquivoAcimaDoTamanhoMaximo_Deve_RetornarGrandeDemais()
    {
        CriarArquivoComTamanho("dados/extrato.csv", OrigemArquivoImportacaoLocal.TamanhoMaximoArquivo + 1);

        Result<Stream, FalhaOrigemArquivoImportacao> result = await _origem.AbrirAsync("dados/extrato.csv", CancellationToken.None);

        result.Should().FailWith(FalhaOrigemArquivoImportacao.GrandeDemais);
    }

    [Fact]
    public async Task AbrirAsync_Quando_RaizNaoExiste_Deve_RetornarOrigemIndisponivel()
    {
        OrigemArquivoImportacaoLocal origem = new(Path.Combine(_repositorio, "inexistente"));

        Result<Stream, FalhaOrigemArquivoImportacao> result = await origem.AbrirAsync("inexistente/extrato.csv", CancellationToken.None);

        result.Should().FailWith(FalhaOrigemArquivoImportacao.OrigemIndisponivel);
    }

    /// <param name="relativo">Relativo à pasta do repositório de teste.</param>
    /// <returns>O caminho completo do arquivo.</returns>
    private string CriarArquivo(string relativo)
    {
        string caminho = Path.Combine(_repositorio, relativo);
        Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
        File.WriteAllText(caminho, Conteudo);
        return caminho;
    }

    private void CriarArquivoComTamanho(string relativo, long tamanho)
    {
        string caminho = CriarArquivo(relativo);
        using FileStream arquivo = File.OpenWrite(caminho);
        arquivo.SetLength(tamanho);
    }

    private static async Task<string> LerAsync(Result<Stream, FalhaOrigemArquivoImportacao> result)
    {
        await using Stream arquivo = result.Should().Succeed().Which;
        using StreamReader leitor = new(arquivo);
        return await leitor.ReadToEndAsync();
    }
}
