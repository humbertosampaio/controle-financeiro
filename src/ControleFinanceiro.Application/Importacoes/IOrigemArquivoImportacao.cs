using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Application.Importacoes;

/// <summary>
/// Porta de onde a ferramenta do servidor MCP abre o arquivo que o usuário indicou pelo caminho: uma pasta só, fora
/// da qual nada se abre.
/// </summary>
/// <remarks>
/// A importação para o modelo recebe o <see cref="Stream"/> já aberto, como a da API, e não conhece o caminho. Assim,
/// um arquivo que chegue por upload segue pela mesma importação, sem passar por esta porta.
/// </remarks>
public interface IOrigemArquivoImportacao
{
    /// <summary>Abre o arquivo para leitura. Quem chama fecha o <see cref="Stream"/>.</summary>
    /// <param name="caminho">Absoluto, ou relativo à pasta que contém a pasta de origem, como o usuário o digita.</param>
    Task<Result<Stream, FalhaOrigemArquivoImportacao>> AbrirAsync(string caminho, CancellationToken cancellationToken);
}
