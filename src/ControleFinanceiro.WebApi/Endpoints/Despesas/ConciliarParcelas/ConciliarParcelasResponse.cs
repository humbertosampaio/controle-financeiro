namespace ControleFinanceiro.WebApi.Endpoints.Despesas.ConciliarParcelas;

/// <param name="Vinculadas">Quantas transações foram ligadas a uma parcela.</param>
/// <param name="SemParcela">Quantas saídas parceladas não acharam parcela livre e serão tentadas de novo na próxima chamada.</param>
public sealed record class ConciliarParcelasResponse(int Vinculadas, int SemParcela);
