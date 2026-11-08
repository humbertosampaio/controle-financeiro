using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.Infrastructure.Persistence.Despesas;

internal sealed class DespesaRepository : IDespesaRepository
{
    private readonly DbSet<Despesa> _dbSet;

    public DespesaRepository(ControleFinanceiroDbContext dbContext)
    {
        _dbSet = dbContext.Despesas;
    }

    public async Task<IReadOnlyList<Despesa>> ListarDoMesAsync(MesReferencia mesReferencia, CancellationToken cancellationToken)
    {
        return await _dbSet
            .Where(d => (d is DespesaInteira && ((DespesaInteira)d).Pagamento.MesReferencia == mesReferencia)
                || (d is DespesaParcelada && ((DespesaParcelada)d).Parcelas.Any(p => p.Pagamento.MesReferencia == mesReferencia)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DespesaParcelada>> ListarParceladasAsync(CancellationToken cancellationToken) =>
        await _dbSet.OfType<DespesaParcelada>().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DespesaAdiada>> ListarAdiadasAsync(CancellationToken cancellationToken) =>
        await _dbSet.OfType<DespesaAdiada>().ToListAsync(cancellationToken);

    public async Task<Maybe<Despesa>> ObterAsync(IdDespesa idDespesa, CancellationToken cancellationToken) =>
        await _dbSet.FindAsync([idDespesa], cancellationToken);

    public async Task<Maybe<Despesa>> ObterPelaCobrancaAsync(IdTransacao idTransacao, CancellationToken cancellationToken)
    {
        return await _dbSet
            .Where(d => (d is DespesaAdiada && ((DespesaAdiada)d).Cobranca!.IdTransacao == idTransacao)
                || (d is DespesaParcelada && ((DespesaParcelada)d).Parcelas.Any(p => p.Cobranca!.IdTransacao == idTransacao)))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Despesa>> ListarPeloPagamentoAsync(IdTransacao idTransacao, CancellationToken cancellationToken)
    {
        return await _dbSet
            .Where(d => (d is DespesaInteira && ((DespesaInteira)d).Pagamento.IdTransacao == idTransacao)
                || (d is DespesaParcelada && ((DespesaParcelada)d).Parcelas.Any(p => p.Pagamento.IdTransacao == idTransacao)))
            .ToListAsync(cancellationToken);
    }

    public async Task AdicionarAsync(IEnumerable<Despesa> despesas, CancellationToken cancellationToken) =>
        await _dbSet.AddRangeAsync(despesas, cancellationToken);
}
