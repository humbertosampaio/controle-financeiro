namespace ControleFinanceiro.WebApi.Endpoints.Categorias.RemoverAlias;

public sealed record class RemoverAliasCategoriaRequest
{
    /// <summary>Vem da rota, que vence o corpo.</summary>
    public Guid IdUsuario { get; init; }

    /// <summary>Vem da rota, que vence o corpo.</summary>
    public Guid IdCategoria { get; init; }

    /// <summary>
    /// O texto da categoria do banco, comparado sem caixa e acentos. Vai no corpo, e não na rota, para o texto do
    /// arquivo não aparecer nos logs e nos traces da requisição.
    /// </summary>
    public string? Alias { get; init; }
}
