namespace ControleFinanceiro.WebApi.Endpoints.Despesas.VincularTransacao;

/// <param name="IdParcela">A parcela cobrada, só na despesa parcelada.</param>
/// <param name="NumeroParcela">O número da parcela cobrada, só na despesa parcelada.</param>
/// <param name="ValorEfetivo">O que de fato saiu, só quando está paga: a simples paga pela saída.</param>
/// <param name="DataEfetiva">A data em que saiu, só quando está paga.</param>
/// <param name="ValorCobrado">O valor da compra da fatura, só na despesa do cartão, que a compra cobra sem pagar.</param>
public sealed record class VincularTransacaoResponse(
    Guid IdDespesa,
    Guid? IdParcela,
    int? NumeroParcela,
    decimal ValorProjetado,
    decimal? ValorEfetivo,
    DateOnly? DataEfetiva,
    decimal? ValorCobrado);
