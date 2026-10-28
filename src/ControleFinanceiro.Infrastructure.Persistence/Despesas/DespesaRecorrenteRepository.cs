using ControleFinanceiro.Domain.Despesas;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.Infrastructure.Persistence.Despesas;

internal sealed class DespesaRecorrenteRepository : IDespesaRecorrenteRepository
{
    private readonly DbSet<DespesaRecorrente> _dbSet;

    public DespesaRecorrenteRepository(ControleFinanceiroDbContext dbContext)
    {
        _dbSet = dbContext.DespesasRecorrentes;
    }

    public async Task<IReadOnlyList<DespesaRecorrente>> ListarAsync(CancellationToken cancellationToken) =>
        await _dbSet.ToListAsync(cancellationToken);

    public async Task AdicionarAsync(DespesaRecorrente despesaRecorrente, CancellationToken cancellationToken) =>
        await _dbSet.AddAsync(despesaRecorrente, cancellationToken);
}
