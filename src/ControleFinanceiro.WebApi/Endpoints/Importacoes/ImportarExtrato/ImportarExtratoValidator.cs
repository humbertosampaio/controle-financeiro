using ControleFinanceiro.Application.Validation;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Usuarios;
using FastEndpoints;
using FluentValidation;

namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarExtrato;

public sealed class ImportarExtratoValidator : Validator<ImportarExtratoRequest>
{
    /// <summary>O maior arquivo aceito (D11 da #6): um extrato de um ano inteiro tem uma fração disso.</summary>
    public const long TamanhoMaximoArquivo = 5 * 1024 * 1024;

    public ImportarExtratoValidator()
    {
        RuleFor(x => x.IdUsuario)
            .MustBeId(IdUsuario.TryFrom);

        RuleFor(x => x.Layout)
            .MustBeSmartEnum(LayoutArquivoImportacao.List);

        RuleFor(x => x.Arquivo)
            .NotNull()
            .WithMessage("O arquivo do extrato é obrigatório.");

        RuleFor(x => x.Arquivo!.Length)
            .LessThanOrEqualTo(TamanhoMaximoArquivo)
            .OverridePropertyName(nameof(ImportarExtratoRequest.Arquivo))
            .WithMessage($"O arquivo do extrato deve ter no máximo {TamanhoMaximoArquivo / (1024 * 1024)} MB.")
            .When(x => x.Arquivo is not null);
    }
}
