namespace ControleFinanceiro.WebApi.Endpoints.Beneficiarios.Listar;

public sealed record class ListarBeneficiariosRequest
{
    /// <summary>Vem da rota.</summary>
    public Guid IdUsuario { get; init; }
}
