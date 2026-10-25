using ControleFinanceiro.Domain.Contas;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.Infrastructure.Persistence.Contas;

internal sealed class ContaRepository : IContaRepository
{
    private readonly DbSet<ContaBancaria> _dbSet;

    public ContaRepository(ControleFinanceiroDbContext dbContext)
    {
        _dbSet = dbContext.Contas;
    }

    public async Task<IReadOnlyList<ContaBancaria>> ListarAsync(CancellationToken cancellationToken) =>
        await _dbSet.ToListAsync(cancellationToken);

    public async Task AdicionarAsync(ContaBancaria conta, CancellationToken cancellationToken) =>
        await _dbSet.AddAsync(conta, cancellationToken);
}
