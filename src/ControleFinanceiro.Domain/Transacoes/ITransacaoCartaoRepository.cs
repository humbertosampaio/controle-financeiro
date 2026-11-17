using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Transacoes;

/// <summary>Enxerga só as transações de cartão do usuário atual.</summary>
public interface ITransacaoCartaoRepository
{
    /// <summary>
    /// Adiciona só as transações cuja chave de deduplicação ainda não está no banco de dados, em nenhuma das origens.
    /// </summary>
    /// <returns>As transações adicionadas: as mesmas instâncias recebidas, na mesma ordem.</returns>
    Task<IReadOnlyList<TransacaoCartao>> AdicionarNovasAsync(IEnumerable<TransacaoCartao> transacoes, CancellationToken cancellationToken);

    /// <summary>Todas as transações com parcela.</summary>
    Task<IReadOnlyList<TransacaoCartao>> ListarParceladasAsync(CancellationToken cancellationToken);

    /// <summary>
    /// As transações trazidas pela fatura que vence no <paramref name="mesVencimento"/>, pelo
    /// <see cref="TransacaoCartao.MesVencimentoFatura"/>, de qualquer cartão.
    /// </summary>
    Task<IReadOnlyList<TransacaoCartao>> ListarFaturaEmAsync(MesReferencia mesVencimento, CancellationToken cancellationToken);
}
