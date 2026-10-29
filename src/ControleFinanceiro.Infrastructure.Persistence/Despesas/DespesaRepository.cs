using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.Infrastructure.Persistence.Despesas;

internal sealed class DespesaRepository : IDespesaRepository
{
    private readonly DbSet<Despesa> _dbSet;

    public DespesaRepository(ControleFinanceiroDbContext dbContext)
    {
        _dbSet = dbContext.Despesas;
    }

    // Enquanto a DespesaParcelada estiver fora do modelo, toda despesa gravada é inteira e tem mês de referência.
    public async Task<IReadOnlyList<Despesa>> ListarDoMesAsync(MesReferencia mesReferencia, CancellationToken cancellationToken)
    {
        return await _dbSet
            .OfType<DespesaInteira>()
            .Where(d => d.MesReferencia == mesReferencia)
            .ToListAsync<Despesa>(cancellationToken);
    }

    public async Task AdicionarAsync(IEnumerable<Despesa> despesas, CancellationToken cancellationToken) =>
        await _dbSet.AddRangeAsync(despesas, cancellationToken);
}
