using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Domain.Despesas;

/// <summary>
/// A despesa lançada na fatura de um cartão, a adiada e a parcelada: a compra do cartão a cobra, e a saída da conta que
/// paga a fatura a paga.
/// </summary>
public interface IDespesaDoCartao
{
    IdCartao IdCartao { get; }

    /// <summary>
    /// Verdadeiro quando a despesa é de um cartão da fatura e a saída só pagou dela pagamentos que vencem no mês da fatura.
    /// </summary>
    bool FoiPagaSoPelaFatura(FaturaCartao fatura, IdTransacao idSaida);

    /// <summary>
    /// Paga pela saída que pagou a fatura, com a data dela e o valor cobrado, o pagamento que vence no mês da fatura,
    /// quando a despesa é de um cartão dela e o pagamento tem cobrança e está pendente. Devolve se pagou: fora disso, a
    /// despesa fica como está.
    /// </summary>
    /// <remarks>
    /// Interno, e implementado de forma explícita, porque só o <see cref="Services.PagamentoFaturaService"/> confere a
    /// saída e a fatura.
    /// </remarks>
    internal bool PagarFatura(FaturaCartao fatura, IdTransacao idSaida, DateOnly dataPagamento);
}
