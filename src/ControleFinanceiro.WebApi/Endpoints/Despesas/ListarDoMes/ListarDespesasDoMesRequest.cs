namespace ControleFinanceiro.WebApi.Endpoints.Despesas.ListarDoMes;

public sealed record class ListarDespesasDoMesRequest
{
    /// <summary>Vem da rota.</summary>
    public Guid IdUsuario { get; init; }

    /// <summary>Vem da query string.</summary>
    public int Ano { get; init; }

    /// <summary>Vem da query string.</summary>
    public int Mes { get; init; }
}
