using ControleFinanceiro.Application.Validation;
using ControleFinanceiro.Domain.Usuarios;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Despesas.ConciliarParcelas;

public sealed class ConciliarParcelasValidator : Validator<ConciliarParcelasRequest>
{
    public ConciliarParcelasValidator()
    {
        RuleFor(x => x.IdUsuario)
            .MustBeId(IdUsuario.TryFrom);
    }
}
