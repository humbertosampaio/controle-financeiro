using ControleFinanceiro.Domain.Shared.Extensions;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Domain.Cartoes;

/// <summary>
/// A fatura que alguma importação trouxe e que ainda espera a saída que a paga: a fatura, o vencimento real, que a
/// importação gravou, e o total que ela cobra, sempre positivo.
/// </summary>
public sealed class FaturaCartaoAberta : ValueObject
{
    /// <summary>A fatura, com o vencimento projetado pelo dia do cartão, como o <see cref="Services.PagamentoFaturaService"/> a monta.</summary>
    public FaturaCartao Fatura { get; }

    /// <summary>O vencimento real, que a importação da fatura gravou.</summary>
    public VencimentoFatura Vencimento { get; }

    /// <summary>
    /// O que a fatura cobra: as compras, as parcelas, os estornos e as tarifas dos cartões dela, sem o pagamento da
    /// anterior, pelo <see cref="TransacaoCartao.CompoeTotalFatura"/>.
    /// </summary>
    public ReaisPositivo Total { get; }

    private FaturaCartaoAberta(FaturaCartao fatura, VencimentoFatura vencimento, ReaisPositivo total)
    {
        Fatura = fatura;
        Vencimento = vencimento;
        Total = total;
    }

    /// <summary>
    /// A fatura aberta com o total das <paramref name="transacoes"/> que a compõem, com o sinal invertido; vazia quando a
    /// fatura não cobra nada, ou quando os estornos passam das compras.
    /// </summary>
    /// <remarks>Interno porque só o <see cref="Services.CandidatosPagamentoFaturaService"/> sabe que a fatura ainda não foi paga.</remarks>
    /// <param name="vencimento">O vencimento real, de uma importação da fatura.</param>
    internal static Maybe<FaturaCartaoAberta> Abrir(FaturaCartao fatura, VencimentoFatura vencimento, IEnumerable<TransacaoCartao> transacoes)
    {
        Reais cobrado = -transacoes.Where(transacao => transacao.CompoeTotalFatura(fatura)).Sum(transacao => transacao.Valor);

        if (cobrado <= 0)
            return Maybe<FaturaCartaoAberta>.None;

        return new FaturaCartaoAberta(fatura, vencimento, cobrado.ComoPositivo());
    }

    /// <summary>Se o pagamento pode ser o desta fatura: o valor é exatamente o total e a data cai na janela do vencimento.</summary>
    public bool AceitaPagamento(ReaisPositivo valor, DateOnly data) => valor == Total && Vencimento.AceitaPagamentoEm(data);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Fatura;
        yield return Vencimento;
        yield return Total;
    }
}
