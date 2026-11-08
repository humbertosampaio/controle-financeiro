using ControleFinanceiro.Application.Validation;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using FastEndpoints;
using FluentValidation;

namespace ControleFinanceiro.WebApi.Endpoints.Categorias.CadastrarLote;

public sealed class CadastrarLoteCategoriasValidator : Validator<CadastrarLoteCategoriasRequest>
{
    /// <summary>
    /// O maior lote aceito: o lote resolve as categorias do banco pendentes de um arquivo, e um arquivo real tem uma
    /// fração disso de categorias distintas.
    /// </summary>
    public const int TamanhoMaximoLote = 500;

    public CadastrarLoteCategoriasValidator()
    {
        RuleFor(x => x.IdUsuario)
            .MustBeId(IdUsuario.TryFrom);

        RuleFor(x => x.Itens)
            .NotEmpty();

        RuleFor(x => x.Itens)
            .Must(itens => itens.Count <= TamanhoMaximoLote)
            .WithMessage($"O lote deve ter no máximo {TamanhoMaximoLote} itens.");

        RuleForEach(x => x.Itens)
            .ChildRules(item =>
            {
                item.RuleFor(x => x.IdCategoria)
                    .MustBeId(IdCategoriaDespesa.TryFrom);

                item.RuleFor(x => x.Nome)
                    .Null()
                    .WithMessage("Informe a categoria existente ou o nome da nova, não os dois")
                    .When(x => x.IdCategoria is not null);

                item.RuleFor(x => x.Nome)
                    .MustBeTextoObrigatorio()
                    .When(x => x.IdCategoria is null);

                item.RuleFor(x => x.Alias)
                    .MustBeValueObject(Alias.Criar)
                    .When(x => x.IdCategoria is not null || x.Alias is not null);
            });
    }
}
