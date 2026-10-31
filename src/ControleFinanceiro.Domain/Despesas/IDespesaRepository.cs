using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Despesas;

/// <summary>Enxerga só as despesas do usuário atual.</summary>
public interface IDespesaRepository
{
    Task<IReadOnlyList<Despesa>> ListarDoMesAsync(MesReferencia mesReferencia, CancellationToken cancellationToken);

    Task<IReadOnlyList<DespesaParcelada>> ListarParceladasAsync(CancellationToken cancellationToken);

    Task<Maybe<Despesa>> ObterAsync(IdDespesa idDespesa, CancellationToken cancellationToken);

    Task AdicionarAsync(IEnumerable<Despesa> despesas, CancellationToken cancellationToken);
}
