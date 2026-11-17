using ControleFinanceiro.Domain.Transacoes;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.Infrastructure.Persistence.Transacoes;

/// <summary>
/// A deduplicação comum ao <see cref="TransacaoContaRepository"/> e ao <see cref="TransacaoCartaoRepository"/>: a chave é
/// conferida na tabela inteira, nas duas origens.
/// </summary>
internal static class TransacoesNovas
{
    /// <summary>
    /// Quantas chaves vão em cada consulta das que já existem, para o <c>IN</c> não crescer com o tamanho do arquivo.
    /// </summary>
    private const int TamanhoBloco = 1000;

    extension(DbSet<Transacao> dbSet)
    {
        /// <summary>Adiciona só as transações cuja chave de deduplicação ainda não está no banco de dados.</summary>
        /// <returns>As transações adicionadas: as mesmas instâncias recebidas, na mesma ordem.</returns>
        internal async Task<IReadOnlyList<T>> AdicionarNovasAsync<T>(IEnumerable<T> transacoes, CancellationToken cancellationToken)
            where T : Transacao
        {
            List<T> lote = transacoes.ToList();

            HashSet<ChaveDeduplicacao> existentes = [];
            foreach (ChaveDeduplicacao[] bloco in lote.Select(t => t.Chave).Chunk(TamanhoBloco))
            {
                List<ChaveDeduplicacao> encontradas = await dbSet
                    .Where(t => bloco.Contains(t.Chave))
                    .Select(t => t.Chave)
                    .ToListAsync(cancellationToken);

                existentes.UnionWith(encontradas);
            }

            List<T> novas = lote.Where(t => !existentes.Contains(t.Chave)).ToList();
            await dbSet.AddRangeAsync(novas, cancellationToken);
            return novas;
        }
    }
}
