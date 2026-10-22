using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Transacoes;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.Infrastructure.Persistence.Transacoes;

internal sealed class TransacaoRepository : ITransacaoRepository
{
    /// <summary>
    /// Quantas chaves vão em cada consulta das que já existem, para o <c>IN</c> não crescer com o tamanho do arquivo.
    /// </summary>
    private const int TamanhoBloco = 1000;

    private readonly DbSet<Transacao> _dbSet;

    public TransacaoRepository(ControleFinanceiroDbContext dbContext)
    {
        _dbSet = dbContext.Transacoes;
    }

    public async Task<IReadOnlyList<Transacao>> AdicionarNovasAsync(IEnumerable<Transacao> transacoes, CancellationToken cancellationToken)
    {
        List<Transacao> lote = transacoes.ToList();

        HashSet<ChaveDeduplicacao> existentes = [];
        foreach (ChaveDeduplicacao[] bloco in lote.Select(t => t.Chave).Chunk(TamanhoBloco))
        {
            List<ChaveDeduplicacao> encontradas = await _dbSet
                .Where(t => bloco.Contains(t.Chave))
                .Select(t => t.Chave)
                .ToListAsync(cancellationToken);

            existentes.UnionWith(encontradas);
        }

        List<Transacao> novas = lote.Where(t => !existentes.Contains(t.Chave)).ToList();
        await _dbSet.AddRangeAsync(novas, cancellationToken);
        return novas;
    }

    public async Task<IReadOnlyList<Transacao>> ListarNoPeriodoAsync(
        OrigemTransacao origem,
        PeriodoExtrato periodo,
        CancellationToken cancellationToken)
    {
        return await DaOrigem(origem)
            .Where(t => t.Data >= periodo.Inicio && t.Data <= periodo.Fim)
            .ToListAsync(cancellationToken);
    }

    // Um filtro por coluna, e não a comparação das duas, para a consulta usar o índice da conta ou o do cartão.
    private IQueryable<Transacao> DaOrigem(OrigemTransacao origem)
    {
        return origem.IdConta is IdConta idConta
            ? _dbSet.Where(t => t.Origem.IdConta == idConta)
            : _dbSet.Where(t => t.Origem.IdCartao == origem.IdCartao);
    }
}
