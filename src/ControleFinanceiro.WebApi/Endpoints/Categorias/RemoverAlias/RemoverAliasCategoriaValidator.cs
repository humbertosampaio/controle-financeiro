using ControleFinanceiro.Application.Validation;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Categorias.RemoverAlias;

public sealed class RemoverAliasCategoriaValidator : Validator<RemoverAliasCategoriaRequest>
{
    public RemoverAliasCategoriaValidator()
    {
        RuleFor(x => x.IdUsuario)
            .MustBeId(IdUsuario.TryFrom);

        RuleFor(x => x.IdCategoria)
            .MustBeId(IdCategoriaDespesa.TryFrom);

        RuleFor(x => x.Alias)
            .MustBeValueObject(Alias.Criar);
    }
}
