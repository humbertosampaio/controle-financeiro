namespace ControleFinanceiro.WebApi.Endpoints.Despesas.ListarRecorrentes;

public sealed record class ListarDespesasRecorrentesRequest
{
    /// <summary>Vem da rota.</summary>
    public Guid IdUsuario { get; init; }
}
