using ControleFinanceiro.Application.Validation;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using FastEndpoints;
using FluentValidation;

namespace ControleFinanceiro.WebApi.Endpoints.Despesas.CadastrarRecorrente;

public sealed class CadastrarDespesaRecorrenteValidator : Validator<CadastrarDespesaRecorrenteRequest>
{
    public CadastrarDespesaRecorrenteValidator()
    {
        RuleFor(x => x.IdUsuario)
            .MustBeId(IdUsuario.TryFrom);

        RuleFor(x => x.IdBeneficiario)
            .MustBeId(IdBeneficiario.TryFrom);

        RuleFor(x => x.Descricao)
            .MustBeTextoObrigatorio();

        RuleFor(x => x.IdCategoria)
            .MustBeId(IdCategoriaDespesa.TryFrom);

        RuleFor(x => x.FormaPagamento)
            .MustBeSmartEnum(FormaPagamento.List);

        RuleFor(x => x.IdCartao)
            .NotNull()
            .WithMessage("Informe o cartão da despesa no crédito")
            .When(x => x.FormaPagamento == FormaPagamento.Credito.Value);

        RuleFor(x => x.IdCartao)
            .Null()
            .WithMessage("Só a despesa no crédito tem cartão")
            .When(x => x.FormaPagamento != FormaPagamento.Credito.Value);

        RuleFor(x => x.IdCartao)
            .MustBeId(IdCartao.TryFrom);

        RuleFor(x => x.Valor)
            .MustBeValueObject(ReaisPositivo.Criar);

        RuleFor(x => x.DiaOcorrencia)
            .MustBeValueObject(DiaOcorrenciaRecorrente.Criar);
    }
}
