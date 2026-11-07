namespace ControleFinanceiro.WebApi.Endpoints.Despesas.VincularTransacao;

/// <param name="IdParcela">A parcela paga, só na despesa parcelada.</param>
/// <param name="NumeroParcela">O número da parcela paga, só na despesa parcelada.</param>
public sealed record class VincularTransacaoResponse(
    Guid IdDespesa,
    Guid? IdParcela,
    int? NumeroParcela,
    decimal ValorProjetado,
    decimal ValorEfetivo,
    DateOnly DataEfetiva);
