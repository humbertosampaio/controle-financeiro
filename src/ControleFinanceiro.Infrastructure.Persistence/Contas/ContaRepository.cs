using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.Infrastructure.Persistence.Contas;

internal sealed class ContaRepository : IContaRepository
{
    private readonly DbSet<ContaBancaria> _dbSet;

    public ContaRepository(ControleFinanceiroDbContext dbContext)
    {
        _dbSet = dbContext.Contas;
    }

    public async Task<IReadOnlyList<ContaBancaria>> ListarDoUsuarioAsync(IdUsuario idUsuario, CancellationToken cancellationToken) =>
        await _dbSet.Where(c => c.IdUsuario == idUsuario).ToListAsync(cancellationToken);

    public async Task AdicionarAsync(ContaBancaria conta, CancellationToken cancellationToken) =>
        await _dbSet.AddAsync(conta, cancellationToken);
}
