using ControleFinanceiro.Domain.Importacoes;

namespace ControleFinanceiro.Domain.Transacoes;

public interface ITransacaoRepository
{
    /// <summary>
    /// Adiciona só as transações cuja chave de deduplicação ainda não está no banco de dados.
    /// </summary>
    /// <returns>As transações adicionadas: as mesmas instâncias recebidas, na mesma ordem.</returns>
    Task<IReadOnlyList<Transacao>> AdicionarNovasAsync(IEnumerable<Transacao> transacoes, CancellationToken cancellationToken);

    /// <summary>
    /// As transações da origem gravadas no banco de dados com a data dentro do período.
    /// </summary>
    Task<IReadOnlyList<Transacao>> ListarNoPeriodoAsync(OrigemTransacao origem, PeriodoExtrato periodo, CancellationToken cancellationToken);
}
