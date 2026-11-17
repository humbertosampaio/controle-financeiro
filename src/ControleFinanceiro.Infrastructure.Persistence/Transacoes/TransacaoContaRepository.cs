using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.Infrastructure.Persistence.Transacoes;

internal sealed class TransacaoContaRepository : ITransacaoContaRepository
{
    private readonly DbSet<Transacao> _dbSet;

    public TransacaoContaRepository(ControleFinanceiroDbContext dbContext)
    {
        _dbSet = dbContext.Transacoes;
    }

    public async Task<IReadOnlyList<TransacaoConta>> AdicionarNovasAsync(IEnumerable<TransacaoConta> transacoes, CancellationToken cancellationToken) =>
        await _dbSet.AdicionarNovasAsync(transacoes, cancellationToken);

    public async Task<IReadOnlyList<TransacaoConta>> ListarNoPeriodoAsync(IdConta idConta, Periodo periodo, CancellationToken cancellationToken)
    {
        return await _dbSet
            .OfType<TransacaoConta>()
            .Where(t => t.IdConta == idConta && t.Data >= periodo.Inicio && t.Data <= periodo.Fim)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TransacaoConta>> ListarDoMesAsync(MesReferencia mesReferencia, CancellationToken cancellationToken)
    {
        return await _dbSet
            .OfType<TransacaoConta>()
            .Where(t => t.Data >= mesReferencia.Inicio && t.Data <= mesReferencia.Fim)
            .ToListAsync(cancellationToken);
    }
}
