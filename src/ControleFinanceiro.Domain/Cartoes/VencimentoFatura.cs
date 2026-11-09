using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Cartoes;

/// <summary>
/// O vencimento de uma fatura: o mês em que ela vence e o dia de vencimento dos cartões dela, que é o mesmo para todos os
/// finais da mesma fatura. A ordem é a da data: primeiro o mês, depois o dia.
/// </summary>
public sealed class VencimentoFatura : ComparableValueObject
{
    public MesReferencia Mes { get; }
    public DiaVencimentoCartao Dia { get; }

    public VencimentoFatura(MesReferencia mes, DiaVencimentoCartao dia)
    {
        Mes = mes;
        Dia = dia;
    }

    /// <summary>O vencimento da fatura do mês seguinte, no mesmo dia.</summary>
    /// <exception cref="InvalidOperationException">Quando o mês é 12/9999, o último que o <see cref="DateOnly"/> representa.</exception>
    public VencimentoFatura Seguinte() => new(Mes.Posterior(), Dia);

    /// <summary>A data em que a fatura vence: o dia de vencimento no mês dela.</summary>
    public static implicit operator DateOnly(VencimentoFatura vencimento) => vencimento.Mes.NoDia(vencimento.Dia);

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return Mes;
        yield return Dia.Value;
    }
}
