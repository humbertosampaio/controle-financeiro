namespace ControleFinanceiro.WebApi.Endpoints.Despesas.GerarDoMes;

public sealed record class GerarDespesasDoMesRequest
{
    /// <summary>Vem da rota, que vence o corpo.</summary>
    public Guid IdUsuario { get; init; }

    public int Ano { get; init; }
    public int Mes { get; init; }
}
