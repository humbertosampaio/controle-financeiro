namespace ControleFinanceiro.WebApi.Endpoints.Despesas.GerarDoMes;

/// <param name="Geradas">Quantas despesas a geração criou: uma por recorrente sem despesa no mês.</param>
/// <param name="JaExistentes">Quantas recorrentes já tinham a despesa do mês e ficaram de fora.</param>
public sealed record class GerarDespesasDoMesResponse(int Geradas, int JaExistentes);
