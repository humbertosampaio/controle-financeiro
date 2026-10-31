using ControleFinanceiro.Application.Validation;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Usuarios;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Despesas.AlterarCategoria;

public sealed class AlterarCategoriaDespesaValidator : Validator<AlterarCategoriaDespesaRequest>
{
    public AlterarCategoriaDespesaValidator()
    {
        RuleFor(x => x.IdUsuario)
            .MustBeId(IdUsuario.TryFrom);

        RuleFor(x => x.IdDespesa)
            .MustBeId(IdDespesa.TryFrom);

        RuleFor(x => x.IdCategoria)
            .MustBeId(IdCategoriaDespesa.TryFrom);
    }
}
