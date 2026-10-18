using ControleFinanceiro.Application.Validation;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using FastEndpoints;
using FluentValidation;

namespace ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;

public sealed class CadastrarUsuarioValidator : Validator<CadastrarUsuarioRequest>
{
    public CadastrarUsuarioValidator()
    {
        RuleFor(x => x.Nome)
            .MustBeTextoObrigatorio();

        RuleFor(x => x.RendaBruta)
            .MustBeValueObject(ReaisPositivo.Criar);

        RuleForEach(x => x.DescontosSalariais)
            .ChildRules(desconto =>
            {
                desconto.RuleFor(x => x.Descricao)
                    .MustBeTextoObrigatorio();

                desconto.RuleFor(x => x.Valor)
                    .MustBeValueObject(ReaisPositivo.Criar);
            });

        RuleFor(x => x.Cartao)
            .NotNull()
            .ChildRules(cartao =>
            {
                cartao.RuleFor(x => x.Sufixo)
                    .MustBeTextoObrigatorio();

                cartao.RuleFor(x => x.DiaVencimento)
                    .MustBeValueObject(DiaVencimentoCartao.Criar);
            });
    }
}