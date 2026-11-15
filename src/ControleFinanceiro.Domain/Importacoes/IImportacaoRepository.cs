using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Domain.Importacoes;

/// <summary>Enxerga só as importações do usuário atual.</summary>
public interface IImportacaoRepository
{
    Task AdicionarAsync(ImportacaoArquivo importacao, CancellationToken cancellationToken);

    /// <summary>
    /// A importação da origem cujo período termina mais tarde, para apontar o período que nenhum arquivo importou.
    /// </summary>
    Task<Maybe<ImportacaoArquivo>> ObterMaisRecenteAsync(OrigemTransacao origem, CancellationToken cancellationToken);

    /// <summary>As importações de fatura com vencimento no <paramref name="mesVencimento"/>, de qualquer cartão.</summary>
    Task<IReadOnlyList<ImportacaoArquivo>> ListarImportacoesFaturaEmAsync(MesReferencia mesVencimento, CancellationToken cancellationToken);
}
