namespace ControleFinanceiro.Domain.Cartoes;

/// <summary>
/// Os 4 últimos dígitos do cartão: o suficiente para reconhecê-lo na fatura, nunca o número completo.
/// </summary>
public sealed class SufixoCartao : SimpleValueObject<string>
{
    public const int QuantidadeDigitos = 4;

    private SufixoCartao(string value) : base(value)
    {
    }

    public static Result<SufixoCartao> Criar(string? sufixo)
    {
        string? valor = sufixo?.Trim();

        return Result
            .SuccessIf(
                valor is { Length: QuantidadeDigitos } && valor.All(char.IsAsciiDigit),
                valor!,
                $"O sufixo do cartão deve ter exatamente {QuantidadeDigitos} dígitos")
            .Map(v => new SufixoCartao(v));
    }

    public static explicit operator SufixoCartao(string? sufixo) => Criar(sufixo).Value;
}
