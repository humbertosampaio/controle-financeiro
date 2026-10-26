namespace ControleFinanceiro.WebApi.Endpoints.Categorias.CadastrarLote;

public sealed record class CadastrarLoteCategoriasRequest
{
    /// <summary>Vem da rota, que vence o corpo.</summary>
    public Guid IdUsuario { get; init; }

    /// <summary>Aplicados todos ou nenhum.</summary>
    public IReadOnlyList<ItemCategoriaDto> Itens { get; init; } = [];
}

/// <summary>
/// Com <see cref="IdCategoria"/>, o <see cref="Alias"/> passa a apontar para essa categoria do usuário. Com
/// <see cref="Nome"/>, uma categoria nova é cadastrada, com o <see cref="Alias"/> quando ele vem de um arquivo.
/// </summary>
public sealed record class ItemCategoriaDto
{
    /// <summary>O texto da categoria do banco. Obrigatório com <see cref="IdCategoria"/>; sem ele, o cadastro é manual.</summary>
    public string? Alias { get; init; }

    public Guid? IdCategoria { get; init; }

    public string? Nome { get; init; }
}
