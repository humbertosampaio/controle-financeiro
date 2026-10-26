using ControleFinanceiro.Domain.Beneficiarios;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.Infrastructure.Persistence.Beneficiarios;

internal sealed class BeneficiarioRepository : IBeneficiarioRepository
{
    private readonly DbSet<Beneficiario> _dbSet;

    public BeneficiarioRepository(ControleFinanceiroDbContext dbContext)
    {
        _dbSet = dbContext.Beneficiarios;
    }

    public async Task<IReadOnlyList<Beneficiario>> ListarAsync(CancellationToken cancellationToken) =>
        await _dbSet.ToListAsync(cancellationToken);

    public async Task<Maybe<Beneficiario>> ObterAsync(IdBeneficiario idBeneficiario, CancellationToken cancellationToken) =>
        await _dbSet.FindAsync([idBeneficiario], cancellationToken);

    public async Task AdicionarAsync(Beneficiario beneficiario, CancellationToken cancellationToken) =>
        await _dbSet.AddAsync(beneficiario, cancellationToken);
}
