using ControleFinanceiro.Application.Abstractions;

namespace ControleFinanceiro.Infrastructure.Persistence;

internal sealed class EntityFrameworkUnitOfWork : IUnitOfWork
{
    private readonly ControleFinanceiroDbContext _controleFinanceiroDbContext;

    public EntityFrameworkUnitOfWork(ControleFinanceiroDbContext dbContext)
    {
        _controleFinanceiroDbContext = dbContext;
    }

    public async Task<int> CommitAsync(CancellationToken cancellationToken) =>
        await _controleFinanceiroDbContext.SaveChangesAsync(cancellationToken);
}
