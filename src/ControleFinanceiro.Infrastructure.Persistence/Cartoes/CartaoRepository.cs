using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.Infrastructure.Persistence.Cartoes;

internal sealed class CartaoRepository : ICartaoRepository
{
    private readonly DbSet<Cartao> _dbSet;

    public CartaoRepository(ControleFinanceiroDbContext dbContext)
    {
        _dbSet = dbContext.Cartoes;
    }

    public async Task<IReadOnlyList<Cartao>> ListarDoUsuarioAsync(IdUsuario idUsuario, CancellationToken cancellationToken) =>
        await _dbSet.Where(c => c.IdUsuario == idUsuario).ToListAsync(cancellationToken);

    public async Task AddAsync(Cartao cartao, CancellationToken cancellationToken) =>
        await _dbSet.AddAsync(cartao, cancellationToken);
}
