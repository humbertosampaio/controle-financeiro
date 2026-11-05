namespace ControleFinanceiro.Domain.Shared.ValueObjects;

/// <summary>
/// O balanço de um mês: a renda líquida do usuário menos o que as despesas do mês já pagaram e o que ainda falta
/// pagar.
/// </summary>
public sealed class BalancoMensal : ValueObject
{
    public MesReferencia Mes { get; }
    public Reais RendaLiquida { get; }

    /// <summary>A soma dos valores efetivos dos pagamentos do mês já pagos.</summary>
    public ReaisPositivo TotalPago { get; }

    /// <summary>A soma dos valores projetados dos pagamentos do mês ainda pendentes.</summary>
    public ReaisPositivo TotalPendente { get; }

    /// <summary>O que sobra da renda líquida depois de pagas todas as despesas do mês, negativo quando elas passam dela.</summary>
    public Reais SaldoPrevisto => RendaLiquida - TotalPago - TotalPendente;

    /// <summary>Interno porque só o <see cref="Services.BalancoMensalService"/> sabe quais pagamentos entram no mês.</summary>
    internal BalancoMensal(MesReferencia mes, Reais rendaLiquida, ReaisPositivo totalPago, ReaisPositivo totalPendente)
    {
        Mes = mes;
        RendaLiquida = rendaLiquida;
        TotalPago = totalPago;
        TotalPendente = totalPendente;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Mes;
        yield return RendaLiquida;
        yield return TotalPago;
        yield return TotalPendente;
    }
}
