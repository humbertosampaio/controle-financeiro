namespace ControleFinanceiro.WebApi.Endpoints.Despesas.AlterarCategoria;

public sealed record class AlterarCategoriaDespesaRequest
{
    /// <summary>Vem da rota, que vence o corpo.</summary>
    public Guid IdUsuario { get; init; }

    /// <summary>Vem da rota, que vence o corpo.</summary>
    public Guid IdDespesa { get; init; }

    public Guid IdCategoria { get; init; }
}
