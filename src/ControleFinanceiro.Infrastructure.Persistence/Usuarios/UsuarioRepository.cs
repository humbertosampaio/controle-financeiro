using ControleFinanceiro.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.Infrastructure.Persistence.Usuarios;

internal sealed class UsuarioRepository : IUsuarioRepository
{
    private readonly DbSet<Usuario> _dbSet;

    public UsuarioRepository(ControleFinanceiroDbContext dbContext)
    {
        _dbSet = dbContext.Usuarios;
    }

    public async Task AddAsync(Usuario usuario, CancellationToken cancellationToken) =>
        await _dbSet.AddAsync(usuario, cancellationToken);
}
