using ControleFinanceiro.Application.Validation;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Usuarios;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Categorias.Renomear;

public sealed class RenomearCategoriaValidator : Validator<RenomearCategoriaRequest>
{
    public RenomearCategoriaValidator()
    {
        RuleFor(x => x.IdUsuario)
            .MustBeId(IdUsuario.TryFrom);

        RuleFor(x => x.IdCategoria)
            .MustBeId(IdCategoriaDespesa.TryFrom);

        RuleFor(x => x.Nome)
            .MustBeTextoObrigatorio();
    }
}
