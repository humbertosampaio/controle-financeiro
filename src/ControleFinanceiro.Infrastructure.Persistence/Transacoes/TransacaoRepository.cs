using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.Infrastructure.Persistence.Transacoes;

internal sealed class TransacaoRepository : ITransacaoRepository
{
    private readonly DbSet<Transacao> _dbSet;

    public TransacaoRepository(ControleFinanceiroDbContext dbContext)
    {
        _dbSet = dbContext.Transacoes;
    }

    public async Task<Maybe<Transacao>> ObterAsync(IdTransacao idTransacao, CancellationToken cancellationToken) =>
        await _dbSet.FindAsync([idTransacao], cancellationToken);

    public async Task<IReadOnlyList<Transacao>> ListarDoMesAsync(MesReferencia mesReferencia, CancellationToken cancellationToken)
    {
        return await _dbSet
            .Where(t => t.Data >= mesReferencia.Inicio && t.Data <= mesReferencia.Fim)
            .OrderBy(t => t.Data)
            .ThenBy(t => t.Id)
            .ToListAsync(cancellationToken);
    }
}
