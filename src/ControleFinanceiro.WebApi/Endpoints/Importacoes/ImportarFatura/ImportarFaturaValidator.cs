using ControleFinanceiro.Application.Validation;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Usuarios;
using FastEndpoints;
using FluentValidation;

namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarFatura;

public sealed class ImportarFaturaValidator : Validator<ImportarFaturaRequest>
{
    /// <summary>O maior arquivo aceito: uma fatura de um mês tem uma fração disso.</summary>
    public const long TamanhoMaximoArquivo = 5 * 1024 * 1024;

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

        RuleFor(x => x.DiaVencimento)
            .MustBeValueObject(dia => DiaVencimentoCartao.Criar(dia!.Value))
            .When(x => x.DiaVencimento is not null);

        RuleFor(x => x.Vencimentos)
            .MustBeValueObject(vencimentos => VencimentosFormulario.Ler(diaUnico: null, vencimentos))
            .When(x => x.Vencimentos is not null);
    }
}
