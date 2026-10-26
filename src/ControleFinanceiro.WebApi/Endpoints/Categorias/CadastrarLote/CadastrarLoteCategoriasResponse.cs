namespace ControleFinanceiro.WebApi.Endpoints.Categorias.CadastrarLote;

/// <param name="IdsCategorias">A categoria de cada item, na ordem dos itens: a existente ou a criada por ele.</param>
public sealed record class CadastrarLoteCategoriasResponse(IReadOnlyList<Guid> IdsCategorias);
