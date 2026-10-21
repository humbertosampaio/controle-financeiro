using ControleFinanceiro.Domain.Importacoes;

namespace ControleFinanceiro.Domain.Transacoes;

public interface ITransacaoRepository
{
    /// <summary>
    /// Adiciona só as transações cuja chave de deduplicação ainda não está no banco de dados.
    /// </summary>
    /// <returns>Quantas transações foram adicionadas.</returns>
    Task<int> AdicionarNovasAsync(IEnumerable<Transacao> transacoes, CancellationToken cancellationToken);

    /// <summary>
    /// As transações da origem gravadas no banco de dados com a data dentro do período.
    /// </summary>
    Task<IReadOnlyList<Transacao>> ListarNoPeriodoAsync(OrigemTransacao origem, PeriodoExtrato periodo, CancellationToken cancellationToken);
}
