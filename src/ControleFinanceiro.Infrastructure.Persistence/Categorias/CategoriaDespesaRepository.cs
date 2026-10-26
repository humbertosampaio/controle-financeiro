using ControleFinanceiro.Domain.Categorias;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.Infrastructure.Persistence.Categorias;

internal sealed class CategoriaDespesaRepository : ICategoriaDespesaRepository
{
    private readonly DbSet<CategoriaDespesa> _dbSet;

    public CategoriaDespesaRepository(ControleFinanceiroDbContext dbContext)
    {
        _dbSet = dbContext.Categorias;
    }

    public async Task<IReadOnlyList<CategoriaDespesa>> ListarAsync(CancellationToken cancellationToken) =>
        await _dbSet.ToListAsync(cancellationToken);

    public async Task<Maybe<CategoriaDespesa>> ObterAsync(IdCategoriaDespesa idCategoria, CancellationToken cancellationToken) =>
        await _dbSet.FindAsync([idCategoria], cancellationToken);

    public async Task AdicionarAsync(CategoriaDespesa categoria, CancellationToken cancellationToken) =>
        await _dbSet.AddAsync(categoria, cancellationToken);
}
