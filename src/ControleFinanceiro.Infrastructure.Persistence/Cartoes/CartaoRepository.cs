using ControleFinanceiro.Domain.Cartoes;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.Infrastructure.Persistence.Cartoes;

internal sealed class CartaoRepository : ICartaoRepository
{
    private readonly DbSet<Cartao> _dbSet;

    public CartaoRepository(ControleFinanceiroDbContext dbContext)
    {
        _dbSet = dbContext.Cartoes;
    }

    public async Task<IReadOnlyList<Cartao>> ListarAsync(CancellationToken cancellationToken) =>
        await _dbSet.ToListAsync(cancellationToken);

    public async Task<Maybe<Cartao>> ObterAsync(IdCartao idCartao, CancellationToken cancellationToken) =>
        await _dbSet.SingleOrDefaultAsync(c => c.Id == idCartao, cancellationToken);

    public async Task AdicionarAsync(Cartao cartao, CancellationToken cancellationToken) =>
        await _dbSet.AddAsync(cartao, cancellationToken);
}
