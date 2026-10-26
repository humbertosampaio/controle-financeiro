using ControleFinanceiro.Domain.Usuarios;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.Infrastructure.Persistence.Usuarios;

internal sealed class UsuarioRepository : IUsuarioRepository
{
    private readonly DbSet<Usuario> _dbSet;

    public UsuarioRepository(ControleFinanceiroDbContext dbContext)
    {
        _dbSet = dbContext.Usuarios;
    }

    public async Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken) =>
        await _dbSet.AddAsync(usuario, cancellationToken);

    public async Task<Maybe<Usuario>> ObterPorIdAsync(IdUsuario id, CancellationToken cancellationToken) =>
        await _dbSet.FindAsync([id], cancellationToken);
}
