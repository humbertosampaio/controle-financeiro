using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Domain.Importacoes;

public interface IImportacaoRepository
{
    Task AdicionarAsync(ImportacaoExtrato importacao, CancellationToken cancellationToken);

    /// <summary>
    /// A importação da origem cujo período termina mais tarde, para apontar o período que nenhum arquivo importou.
    /// </summary>
    Task<Maybe<ImportacaoExtrato>> ObterMaisRecenteAsync(OrigemTransacao origem, CancellationToken cancellationToken);
}
