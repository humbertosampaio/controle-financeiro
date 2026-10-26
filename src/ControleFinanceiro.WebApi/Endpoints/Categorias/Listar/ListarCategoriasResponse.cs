namespace ControleFinanceiro.WebApi.Endpoints.Categorias.Listar;

/// <param name="Categorias">Todas as categorias do usuário, inclusive a Outros, em ordem alfabética de nome.</param>
public sealed record class ListarCategoriasResponse(IReadOnlyList<ListarCategoriasResponse.Categoria> Categorias)
{
    /// <param name="Aliases">As categorias do banco associadas a ela, como vieram do arquivo, na ordem em que entraram.</param>
    public sealed record class Categoria(Guid Id, string Nome, IReadOnlyList<string> Aliases);
}
