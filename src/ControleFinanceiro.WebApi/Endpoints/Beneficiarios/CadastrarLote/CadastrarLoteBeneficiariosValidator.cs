using ControleFinanceiro.Application.Validation;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using FastEndpoints;
using FluentValidation;

namespace ControleFinanceiro.WebApi.Endpoints.Beneficiarios.CadastrarLote;

public sealed class CadastrarLoteBeneficiariosValidator : Validator<CadastrarLoteBeneficiariosRequest>
{
    /// <summary>
    /// O maior lote aceito: o lote resolve os beneficiários pendentes de um arquivo, e um extrato de um ano inteiro tem
    /// uma fração disso de contrapartes distintas.
    /// </summary>
    public const int TamanhoMaximoLote = 500;

    public CadastrarLoteBeneficiariosValidator()
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
                item.RuleFor(x => x.IdBeneficiario)
                    .MustBeId(id => IdBeneficiario.TryFrom(id!.Value))
                    .When(x => x.IdBeneficiario is not null);

                item.RuleFor(x => x.Nome)
                    .Null()
                    .WithMessage("Informe o beneficiário existente ou o nome do novo, não os dois")
                    .When(x => x.IdBeneficiario is not null);

                item.RuleFor(x => x.Nome)
                    .MustBeTextoObrigatorio()
                    .When(x => x.IdBeneficiario is null);

                item.RuleFor(x => x.Alias)
                    .MustBeValueObject(Alias.Criar)
                    .When(x => x.IdBeneficiario is not null || x.Alias is not null);
            });
    }
}
