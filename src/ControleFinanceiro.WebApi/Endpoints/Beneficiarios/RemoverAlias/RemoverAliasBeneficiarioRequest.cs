namespace ControleFinanceiro.WebApi.Endpoints.Beneficiarios.RemoverAlias;

public sealed record class RemoverAliasBeneficiarioRequest
{
    /// <summary>Vem da rota, que vence o corpo.</summary>
    public Guid IdUsuario { get; init; }

    /// <summary>Vem da rota, que vence o corpo.</summary>
    public Guid IdBeneficiario { get; init; }

    /// <summary>
    /// O texto de beneficiário do arquivo, comparado sem caixa e acentos. Vai no corpo, e não na rota, para o texto do
    /// arquivo não aparecer nos logs e nos traces da requisição.
    /// </summary>
    public string? Alias { get; init; }
}
