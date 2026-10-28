namespace ControleFinanceiro.WebApi.Endpoints.Beneficiarios.CadastrarLote;

public sealed record class CadastrarLoteBeneficiariosRequest
{
    /// <summary>Vem da rota, que vence o corpo.</summary>
    public Guid IdUsuario { get; init; }

    /// <summary>Aplicados todos ou nenhum.</summary>
    public IReadOnlyList<ItemBeneficiarioDto> Itens { get; init; } = [];
}

/// <summary>
/// Com <see cref="IdBeneficiario"/>, o <see cref="Alias"/> passa a apontar para esse beneficiário do usuário. Com
/// <see cref="Nome"/>, um beneficiário novo é cadastrado, com o <see cref="Alias"/> quando ele vem de um arquivo.
/// </summary>
public sealed record class ItemBeneficiarioDto
{
    /// <summary>
    /// O texto de beneficiário do arquivo. Obrigatório com <see cref="IdBeneficiario"/>; sem ele, o cadastro é manual.
    /// </summary>
    public string? Alias { get; init; }

    public Guid? IdBeneficiario { get; init; }

    public string? Nome { get; init; }
}
