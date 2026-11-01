using ControleFinanceiro.Domain.Importacoes;

namespace ControleFinanceiro.Domain.Transacoes;

/// <summary>Enxerga só as transações do usuário atual.</summary>
public interface ITransacaoRepository
{
    Task<Maybe<Transacao>> ObterAsync(IdTransacao idTransacao, CancellationToken cancellationToken);

    /// <summary>Todas as transações com parcela.</summary>
    Task<IReadOnlyList<Transacao>> ListarParceladasAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Adiciona só as transações cuja chave de deduplicação ainda não está no banco de dados.
    /// </summary>
    /// <returns>As transações adicionadas: as mesmas instâncias recebidas, na mesma ordem.</returns>
    Task<IReadOnlyList<Transacao>> AdicionarNovasAsync(IEnumerable<Transacao> transacoes, CancellationToken cancellationToken);

    /// <summary>
    /// As transações da origem gravadas no banco de dados com a data dentro do período.
    /// </summary>
    Task<IReadOnlyList<Transacao>> ListarNoPeriodoAsync(OrigemTransacao origem, PeriodoArquivo periodo, CancellationToken cancellationToken);
}
