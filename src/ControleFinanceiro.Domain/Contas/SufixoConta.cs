namespace ControleFinanceiro.Domain.Contas;

/// <summary>
/// Os 4 últimos caracteres do número da conta, com o dígito verificador: o suficiente para distinguir
/// duas contas no mesmo banco, nunca o número completo. O dígito verificador pode ser <c>X</c>.
/// </summary>
public sealed class SufixoConta : SimpleValueObject<string>
{
    public const int QuantidadeCaracteres = 4;

    private static readonly char[] _separadores = ['-', '.', ' '];

    private SufixoConta(string value) : base(value)
    {
    }

    public static Result<SufixoConta> Criar(string? sufixo)
    {
        string? valor = sufixo is null
            ? null
            : string.Concat(sufixo.Where(c => !_separadores.Contains(c))).ToUpperInvariant();

        return Result
            .SuccessIf(
                valor is { Length: QuantidadeCaracteres }
                    && TodosCaracteresExcetoVerificadorSaoNumericos(valor)
                    && DigitoVerificadorNumericoOuX(valor),
                valor!,
                $"O sufixo da conta deve ter {QuantidadeCaracteres} caracteres: dígitos, com X permitido só no dígito verificador")
            .Map(v => new SufixoConta(v));
    }

    private static bool TodosCaracteresExcetoVerificadorSaoNumericos(string valor) => valor[..^1].All(char.IsAsciiDigit);
    private static bool DigitoVerificadorNumericoOuX(string valor) => char.IsAsciiDigit(valor[^1]) || valor[^1] == 'X';

    public static explicit operator SufixoConta(string? sufixo) => Criar(sufixo).Value;
}
