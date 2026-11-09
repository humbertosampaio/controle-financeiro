using ControleFinanceiro.Domain.Shared.Extensions;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Despesas;

/// <summary>O que um pagamento da fatura fez.</summary>
public sealed record class PagamentoFatura
{
    /// <summary>
    /// As despesas que a saída pagou nesta chamada, inteiras ou pela parcela do mês. Nunca vazia: sem nada a pagar, o
    /// pagamento falha.
    /// </summary>
    public IReadOnlyList<Despesa> Pagas { get; }

    /// <summary>O mês em que a fatura vence, em que cada despesa paga tem o pagamento que a saída pagou.</summary>
    public MesReferencia MesVencimento { get; }

    /// <summary>O que a saída pagou nesta chamada: a soma do valor cobrado das despesas e das parcelas do mês.</summary>
    /// <remarks>O <c>Value</c> é seguro: cada despesa paga tem pagamento no mês de vencimento.</remarks>
    public ReaisPositivo Total => Pagas.Sum(despesa => despesa.ValorVigenteDoMes(MesVencimento).Value);

    /// <remarks>
    /// Interno porque só o <see cref="Services.PagamentoFaturaService"/>, que acabou de pagar as despesas no mês, garante
    /// que a lista não é vazia e que cada uma tem pagamento nele.
    /// </remarks>
    internal PagamentoFatura(IReadOnlyList<Despesa> pagas, MesReferencia mesVencimento)
    {
        Pagas = pagas;
        MesVencimento = mesVencimento;
    }
}
