using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.Infrastructure.Persistence.Transacoes;

internal sealed class TransacaoCartaoRepository : ITransacaoCartaoRepository
{
    private readonly DbSet<Transacao> _dbSet;

    public TransacaoCartaoRepository(ControleFinanceiroDbContext dbContext)
    {
        _dbSet = dbContext.Transacoes;
    }

    public async Task<IReadOnlyList<TransacaoCartao>> AdicionarNovasAsync(IEnumerable<TransacaoCartao> transacoes, CancellationToken cancellationToken) =>
        await _dbSet.AdicionarNovasAsync(transacoes, cancellationToken);

    public async Task<IReadOnlyList<TransacaoCartao>> ListarParceladasAsync(CancellationToken cancellationToken) =>
        await _dbSet.OfType<TransacaoCartao>().Where(t => t.Parcela != null).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TransacaoCartao>> ListarFaturaEmAsync(MesReferencia mesVencimento, CancellationToken cancellationToken)
    {
        return await _dbSet
            .OfType<TransacaoCartao>()
            .Where(t => t.MesVencimentoFatura == mesVencimento)
            .ToListAsync(cancellationToken);
    }
}
