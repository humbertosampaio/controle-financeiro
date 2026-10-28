namespace ControleFinanceiro.Domain.Despesas;

/// <summary>Enxerga só as despesas recorrentes do usuário atual.</summary>
public interface IDespesaRecorrenteRepository
{
    Task<IReadOnlyList<DespesaRecorrente>> ListarAsync(CancellationToken cancellationToken);

    Task AdicionarAsync(DespesaRecorrente despesaRecorrente, CancellationToken cancellationToken);
}
