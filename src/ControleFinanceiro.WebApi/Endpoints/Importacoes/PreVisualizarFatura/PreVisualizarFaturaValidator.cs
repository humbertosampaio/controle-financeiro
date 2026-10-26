using ControleFinanceiro.Application.Validation;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarFatura;
using FastEndpoints;
using FluentValidation;

namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.PreVisualizarFatura;

/// <summary>As mesmas regras da importação da fatura para o layout e o arquivo, para a prévia aceitar o que ela aceita.</summary>
public sealed class PreVisualizarFaturaValidator : Validator<PreVisualizarFaturaRequest>
{
    public const long TamanhoMaximoArquivo = ImportarFaturaValidator.TamanhoMaximoArquivo;

    public PreVisualizarFaturaValidator()
    {
        RuleFor(x => x.IdUsuario)
            .MustBeId(IdUsuario.TryFrom);

        RuleFor(x => x.Layout)
            .MustBeSmartEnum(LayoutArquivoImportacao.Faturas);

        RuleFor(x => x.Arquivo)
            .NotNull()
            .WithMessage("O arquivo da fatura é obrigatório.");

        RuleFor(x => x.Arquivo)
            .Must(arquivo => arquivo!.Length <= TamanhoMaximoArquivo)
            .WithMessage($"O arquivo da fatura deve ter no máximo {TamanhoMaximoArquivo / (1024 * 1024)} MB.")
            .When(x => x.Arquivo is not null);
    }
}
