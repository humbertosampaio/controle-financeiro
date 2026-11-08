namespace ControleFinanceiro.WebApi.Endpoints.Despesas.PagarFatura;

/// <param name="Pagas">As despesas que a saída pagou nesta chamada, inteiras ou pela parcela do mês.</param>
public sealed record class PagarFaturaResponse(IReadOnlyList<PagarFaturaResponse.DespesaPaga> Pagas)
{
    /// <param name="ValorEfetivo">O valor cobrado na fatura, que passa a ser o pago.</param>
    /// <param name="DataEfetiva">A data da saída que pagou a fatura.</param>
    /// <param name="Parcela">A parcela paga, só na despesa parcelada.</param>
    public sealed record class DespesaPaga(
        Guid IdDespesa,
        decimal ValorEfetivo,
        DateOnly DataEfetiva,
        PagarFaturaResponse.ParcelaPaga? Parcela);

    /// <param name="Total">O total de parcelas da compra.</param>
    public sealed record class ParcelaPaga(Guid Id, int Numero, int Total);
}
