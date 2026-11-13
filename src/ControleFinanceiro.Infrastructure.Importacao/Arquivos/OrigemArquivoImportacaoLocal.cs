using ControleFinanceiro.Application.Importacoes;
using RaizEArquivo = (string Raiz, string Arquivo);

namespace ControleFinanceiro.Infrastructure.Importacao.Arquivos;

/// <summary>
/// Abre os arquivos de uma pasta local, a raiz da origem. A raiz que é um link vale pelo alvo final, e nada fora
/// dela se abre: nem por <c>..</c>, nem por uma pasta irmã com o mesmo prefixo, nem por um link no caminho.
/// </summary>
/// <remarks>
/// Riscos aceitos, por ser uma ferramenta de um usuário só: o hardlink criado pelo próprio usuário e a janela entre
/// a conferência do caminho e a abertura do arquivo.
/// </remarks>
public sealed class OrigemArquivoImportacaoLocal : IOrigemArquivoImportacao
{
    /// <summary>O mesmo limite dos validators da importação pela API.</summary>
    public const long TamanhoMaximoArquivo = 5 * 1024 * 1024;

    private const string ExtensaoAceita = ".csv";

    private readonly string _raizConfigurada;

    /// <param name="raiz">A pasta de origem. Não precisa existir agora: sem ela, toda abertura falha.</param>
    public OrigemArquivoImportacaoLocal(string raiz)
    {
        _raizConfigurada = Path.TrimEndingDirectorySeparator(Path.GetFullPath(raiz));
    }

    public Task<Result<Stream, FalhaOrigemArquivoImportacao>> AbrirAsync(string caminho, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(Abrir(caminho));
    }

    private Result<Stream, FalhaOrigemArquivoImportacao> Abrir(string caminho)
    {
        return ResolverRaiz()
            .Bind(raiz => Localizar(raiz, caminho).Map(arquivo => (Raiz: raiz, Arquivo: arquivo)))
            .Ensure(NenhumLinkAteOArquivo, FalhaOrigemArquivoImportacao.ForaDaOrigem)
            .Map(raizEArquivo => new FileInfo(raizEArquivo.Arquivo))
            .Ensure(arquivo => arquivo.Extension.Equals(ExtensaoAceita, StringComparison.OrdinalIgnoreCase), FalhaOrigemArquivoImportacao.ExtensaoNaoAceita)
            .Ensure(arquivo => arquivo.Exists, FalhaOrigemArquivoImportacao.NaoEncontrado)
            .Ensure(arquivo => arquivo.Length <= TamanhoMaximoArquivo, FalhaOrigemArquivoImportacao.GrandeDemais)
            .Map(arquivo => (Stream)new FileStream(arquivo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read));
    }

    /// <returns>O caminho completo da raiz, com o link resolvido até o alvo final.</returns>
    private Result<string, FalhaOrigemArquivoImportacao> ResolverRaiz()
    {
        if (!Directory.Exists(_raizConfigurada))
            return FalhaOrigemArquivoImportacao.OrigemIndisponivel;

        FileSystemInfo? alvo = new DirectoryInfo(_raizConfigurada).ResolveLinkTarget(returnFinalTarget: true);
        return Path.TrimEndingDirectorySeparator(alvo?.FullName ?? _raizConfigurada);
    }

    /// <summary>
    /// O caminho do arquivo dentro da <paramref name="raiz"/>. O relativo parte da pasta que contém a raiz
    /// configurada, para que <c>dados/x.csv</c> funcione como o usuário digita; o absoluto pode apontar para a raiz
    /// configurada ou para o alvo dela.
    /// </summary>
    /// <param name="raiz">A raiz com o link resolvido.</param>
    private Result<string, FalhaOrigemArquivoImportacao> Localizar(string raiz, string caminho)
    {
        if (string.IsNullOrWhiteSpace(caminho) || caminho.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            return FalhaOrigemArquivoImportacao.ForaDaOrigem;

        string completo = Path.GetFullPath(caminho, Path.GetDirectoryName(_raizConfigurada)!);

        return RelativoDentroDe(_raizConfigurada, completo)
            .Or(() => RelativoDentroDe(raiz, completo))
            .Map(relativo => Path.Combine(raiz, relativo))
            .ToResult(FalhaOrigemArquivoImportacao.ForaDaOrigem);
    }

    /// <returns>Vazio quando o <paramref name="completo"/> não está abaixo da <paramref name="pasta"/>.</returns>
    private static Maybe<string> RelativoDentroDe(string pasta, string completo)
    {
        string relativo = Path.GetRelativePath(pasta, completo);
        bool sai = relativo == "."
            || relativo == ".."
            || relativo.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || Path.IsPathRooted(relativo);

        return sai ? Maybe<string>.None : relativo;
    }

    /// <summary>Nem o arquivo nem as pastas entre ele e a raiz podem ser symlink ou junction.</summary>
    private static bool NenhumLinkAteOArquivo(RaizEArquivo raizEArquivo)
    {
        if (new FileInfo(raizEArquivo.Arquivo).LinkTarget is not null)
            return false;

        for (string? pasta = Path.GetDirectoryName(raizEArquivo.Arquivo);
             pasta is not null && pasta.Length > raizEArquivo.Raiz.Length;
             pasta = Path.GetDirectoryName(pasta))
        {
            if (new DirectoryInfo(pasta).LinkTarget is not null)
                return false;
        }

        return true;
    }
}
