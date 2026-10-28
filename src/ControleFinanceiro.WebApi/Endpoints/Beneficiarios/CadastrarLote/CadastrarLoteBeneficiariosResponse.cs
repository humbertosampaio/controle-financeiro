namespace ControleFinanceiro.WebApi.Endpoints.Beneficiarios.CadastrarLote;

/// <param name="IdsBeneficiarios">O beneficiário de cada item, na ordem dos itens: o existente ou o criado por ele.</param>
public sealed record class CadastrarLoteBeneficiariosResponse(IReadOnlyList<Guid> IdsBeneficiarios);
