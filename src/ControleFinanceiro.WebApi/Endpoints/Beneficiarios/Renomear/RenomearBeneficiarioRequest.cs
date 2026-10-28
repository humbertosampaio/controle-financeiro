namespace ControleFinanceiro.WebApi.Endpoints.Beneficiarios.Renomear;

public sealed record class RenomearBeneficiarioRequest
{
    /// <summary>Vem da rota, que vence o corpo.</summary>
    public Guid IdUsuario { get; init; }

    /// <summary>Vem da rota, que vence o corpo.</summary>
    public Guid IdBeneficiario { get; init; }

    public string? Nome { get; init; }
}
