using ControleFinanceiro.Application.Validation;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Usuarios;
using FastEndpoints;
using FluentValidation;

namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarExtratoMercadoPago;

public sealed class ImportarExtratoMercadoPagoValidator : Validator<ImportarExtratoMercadoPagoRequest>
{
    /// <summary>O maior arquivo aceito: um extrato de um ano inteiro tem uma fração disso.</summary>
    public const long TamanhoMaximoArquivo = 5 * 1024 * 1024;

    public ImportarExtratoMercadoPagoValidator()
    {
        RuleFor(x => x.IdUsuario)
            .MustBeId(IdUsuario.TryFrom);

        RuleFor(x => x.Arquivo)
            .NotNull()
            .WithMessage("O arquivo do extrato é obrigatório.");

        RuleFor(x => x.Arquivo)
            .Must(arquivo => arquivo!.Length <= TamanhoMaximoArquivo)
            .WithMessage($"O arquivo do extrato deve ter no máximo {TamanhoMaximoArquivo / (1024 * 1024)} MB.")
            .When(x => x.Arquivo is not null);

        RuleFor(x => x.SufixoConta)
            .MustBeValueObject(SufixoConta.Criar);
    }
}
