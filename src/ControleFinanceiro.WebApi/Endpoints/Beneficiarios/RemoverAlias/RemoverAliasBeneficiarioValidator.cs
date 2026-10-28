using ControleFinanceiro.Application.Validation;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Beneficiarios.RemoverAlias;

public sealed class RemoverAliasBeneficiarioValidator : Validator<RemoverAliasBeneficiarioRequest>
{
    public RemoverAliasBeneficiarioValidator()
    {
        RuleFor(x => x.IdUsuario)
            .MustBeId(IdUsuario.TryFrom);

        RuleFor(x => x.IdBeneficiario)
            .MustBeId(IdBeneficiario.TryFrom);

        RuleFor(x => x.Alias)
            .MustBeValueObject(Alias.Criar);
    }
}
