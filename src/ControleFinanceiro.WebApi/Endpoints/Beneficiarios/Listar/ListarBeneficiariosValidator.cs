using ControleFinanceiro.Application.Validation;
using ControleFinanceiro.Domain.Usuarios;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Beneficiarios.Listar;

public sealed class ListarBeneficiariosValidator : Validator<ListarBeneficiariosRequest>
{
    public ListarBeneficiariosValidator()
    {
        RuleFor(x => x.IdUsuario)
            .MustBeId(IdUsuario.TryFrom);
    }
}
