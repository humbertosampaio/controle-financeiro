using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Importacoes;

/// <summary>Enxerga só as importações de fatura do usuário atual.</summary>
public interface IImportacaoFaturaRepository
{
    Task AdicionarAsync(ImportacaoFatura importacao, CancellationToken cancellationToken);

    /// <summary>As importações de fatura com vencimento no <paramref name="mesVencimento"/>, de qualquer cartão.</summary>
    Task<IReadOnlyList<ImportacaoFatura>> ListarEmAsync(MesReferencia mesVencimento, CancellationToken cancellationToken);
}
