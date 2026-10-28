using ControleFinanceiro.Application.Validation;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Usuarios;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Beneficiarios.Renomear;

public sealed class RenomearBeneficiarioValidator : Validator<RenomearBeneficiarioRequest>
{
    public RenomearBeneficiarioValidator()
    {
        RuleFor(x => x.IdUsuario)
            .MustBeId(IdUsuario.TryFrom);

        RuleFor(x => x.IdBeneficiario)
            .MustBeId(IdBeneficiario.TryFrom);

        RuleFor(x => x.Nome)
            .MustBeTextoObrigatorio();
    }
}
