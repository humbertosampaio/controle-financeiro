using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Application.Importacoes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Mcp.Ferramentas;

/// <summary>
/// Abre pela <see cref="IOrigemArquivoImportacao"/> o arquivo que o usuário indicou e entrega o <see cref="Stream"/> à
/// importação, que não conhece o caminho. A falha da origem vira a mensagem fixa, sem repetir o caminho.
/// </summary>
internal static class ArquivoDaOrigem
{
    public static async Task<ImportacaoParaModelo> ImportarAsync(
        IOrigemArquivoImportacao origem,
        string caminho,
        Func<Stream, Task<ImportacaoParaModelo>> importar,
        CancellationToken cancellationToken)
    {
        Result<Stream, FalhaOrigemArquivoImportacao> aberto = await origem.AbrirAsync(caminho, cancellationToken);
        if (aberto.IsFailure)
            return ImportacaoParaModelo.Falha(ErrosParaModelo.Importacao.De(aberto.Error));

        await using Stream arquivo = aberto.Value;
        return await importar(arquivo);
    }
}
