using System.Text;

namespace ControleFinanceiro.Infrastructure.Importacao.Tests;

/// <summary>
/// Um arquivo fictício de <c>Fixtures/</c> e as variações dele que os testes de um leitor montam, no mesmo layout de
/// bytes que o banco exporta: UTF-8, com ou sem BOM, e o fim de linha do banco.
/// </summary>
internal sealed class ArquivoFixture
{
    private readonly string _fimLinha;
    private readonly bool _comBom;

    /// <param name="pasta">A pasta do banco dentro de <c>Fixtures/</c>.</param>
    /// <param name="fimLinha"><c>\r\n</c> ou <c>\n</c>, como o banco exporta.</param>
    /// <param name="comBom">Se o banco exporta o arquivo com o BOM do UTF-8.</param>
    public ArquivoFixture(string pasta, string nome, string fimLinha, bool comBom)
    {
        Caminho = Path.Combine(AppContext.BaseDirectory, "Fixtures", pasta, nome);
        _fimLinha = fimLinha;
        _comBom = comBom;
    }

    public string Caminho { get; }

    /// <summary>As linhas do arquivo, sem o BOM.</summary>
    public string[] Linhas() => File.ReadAllText(Caminho, Encoding.UTF8).Split(_fimLinha);

    /// <summary>O arquivo com uma linha trocada.</summary>
    /// <param name="numero">O número da linha no arquivo, a partir de 1.</param>
    public Stream ComLinha(int numero, string linha)
    {
        string[] linhas = Linhas();
        linhas[numero - 1] = linha;
        return ComLinhas(linhas);
    }

    /// <summary>Um arquivo só com estas <paramref name="linhas"/>, no mesmo layout de bytes.</summary>
    public Stream ComLinhas(IEnumerable<string> linhas)
    {
        byte[] conteudo = Encoding.UTF8.GetBytes(string.Join(_fimLinha, linhas));
        return new MemoryStream(_comBom ? [.. Encoding.UTF8.Preamble, .. conteudo] : conteudo);
    }
}
