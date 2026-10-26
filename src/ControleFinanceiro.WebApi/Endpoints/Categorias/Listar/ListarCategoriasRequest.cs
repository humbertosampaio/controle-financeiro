namespace ControleFinanceiro.WebApi.Endpoints.Categorias.Listar;

public sealed record class ListarCategoriasRequest
{
    /// <summary>Vem da rota.</summary>
    public Guid IdUsuario { get; init; }
}
