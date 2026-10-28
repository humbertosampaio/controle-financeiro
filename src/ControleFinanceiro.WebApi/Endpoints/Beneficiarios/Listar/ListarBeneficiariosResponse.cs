namespace ControleFinanceiro.WebApi.Endpoints.Beneficiarios.Listar;

/// <param name="Beneficiarios">Todos os beneficiários do usuário, em ordem alfabética de nome.</param>
public sealed record class ListarBeneficiariosResponse(IReadOnlyList<ListarBeneficiariosResponse.Beneficiario> Beneficiarios)
{
    /// <param name="Aliases">Os textos de beneficiário do arquivo associados a ele, como vieram, na ordem em que entraram.</param>
    public sealed record class Beneficiario(Guid Id, string Nome, IReadOnlyList<string> Aliases);
}
