namespace ControleFinanceiro.WebApi.Endpoints.Categorias.Renomear;

public sealed record class RenomearCategoriaRequest
{
    /// <summary>Vem da rota, que vence o corpo.</summary>
    public Guid IdUsuario { get; init; }

    /// <summary>Vem da rota, que vence o corpo.</summary>
    public Guid IdCategoria { get; init; }

    public string? Nome { get; init; }
}
