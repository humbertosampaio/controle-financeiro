namespace ControleFinanceiro.WebApi.Endpoints.Despesas.ConciliarParcelas;

public sealed record class ConciliarParcelasRequest
{
    /// <summary>Vem da rota.</summary>
    public Guid IdUsuario { get; init; }
}
