using ControleFinanceiro.Application.Validation;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Usuarios;
using FastEndpoints;
using FluentValidation;

namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarFatura;

public sealed class ImportarFaturaValidator : Validator<ImportarFaturaRequest>
{
    /// <summary>O maior arquivo aceito: uma fatura de um mês tem uma fração disso.</summary>
    public const long TamanhoMaximoArquivo = 5 * 1024 * 1024;

    public const string MensagemVencimentoObrigatorio = "O vencimento da fatura é obrigatório no layout que não o traz no arquivo.";

    public const string MensagemVencimentoNoArquivo = "O layout traz o vencimento no arquivo: não informe o vencimento da fatura.";

    public ImportarFaturaValidator()
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

        RuleFor(x => x.DataVencimento)
            .NotNull()
            .WithMessage(MensagemVencimentoObrigatorio)
            .When(x => EhVencimentoInformado(x.Layout));

        RuleFor(x => x.DataVencimento)
            .Null()
            .WithMessage(MensagemVencimentoNoArquivo)
            .When(x => EhVencimentoNoArquivo(x.Layout));
    }

    /// <summary>Se o código é de um layout de fatura sem o vencimento no arquivo.</summary>
    public static bool EhVencimentoInformado(string layout) => LayoutArquivoImportacao.FaturasVencimentoInformado.Any(l => l.Value == layout);

    /// <summary>Se o código é de um layout de fatura que traz o vencimento no arquivo.</summary>
    public static bool EhVencimentoNoArquivo(string layout) =>
        LayoutArquivoImportacao.Faturas.Any(l => l.Value == layout) && !EhVencimentoInformado(layout);
}
