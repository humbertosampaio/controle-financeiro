using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Domain.Despesas;

/// <summary>Enxerga só as despesas do usuário atual.</summary>
public interface IDespesaRepository
{
    Task<IReadOnlyList<Despesa>> ListarDoMesAsync(MesReferencia mesReferencia, CancellationToken cancellationToken);

    Task<IReadOnlyList<DespesaParcelada>> ListarParceladasAsync(CancellationToken cancellationToken);

    Task<Maybe<Despesa>> ObterAsync(IdDespesa idDespesa, CancellationToken cancellationToken);

    /// <summary>A despesa que a transação pagou, inteira ou por uma das parcelas.</summary>
    Task<Maybe<Despesa>> ObterPelaTransacaoAsync(IdTransacao idTransacao, CancellationToken cancellationToken);

    Task AdicionarAsync(IEnumerable<Despesa> despesas, CancellationToken cancellationToken);
}
