using System.Globalization;

namespace ControleFinanceiro.Domain.Shared.ValueObjects;

/// <summary>
/// Representa um valor monetário em Reais (BRL), arredondado para centavos na construção, com o
/// arredondamento comercial (meio centavo sobe). O zero fica sempre sem sinal: o <see cref="decimal"/> guarda o
/// sinal do zero (<c>0m - 0.00m</c> é negativo), e o valor zero não tem sentido de entrada nem de saída.
/// </summary>
public class Reais(decimal valor) : SimpleValueObject<decimal>(Arredondar(valor))
{
    public const int CasasDecimais = 2;

    internal static readonly ReaisFormatProvider FormatProvider = new();

    internal static decimal Arredondar(decimal valor)
    {
        decimal arredondado = decimal.Round(valor, CasasDecimais, MidpointRounding.AwayFromZero);
        return arredondado == 0m ? decimal.Abs(arredondado) : arredondado;
    }

    public override string ToString() => Value.ToString("C2", FormatProvider);

    public static Reais operator +(Reais a, Reais b) => new(a.Value + b.Value);
    public static Reais operator -(Reais a, Reais b) => new(a.Value - b.Value);

    public static implicit operator Reais(decimal valor) => new(valor);
    public static implicit operator decimal(Reais reais) => reais.Value;

    internal sealed class ReaisFormatProvider : IFormatProvider
    {
        private readonly NumberFormatInfo _numberFormatInfo;

        public ReaisFormatProvider()
        {
            _numberFormatInfo = (NumberFormatInfo)AppDefaults.CulturaPtBr.NumberFormat.Clone();
            _numberFormatInfo.CurrencyPositivePattern = 2;
        }

        public object? GetFormat(Type? formatType) => _numberFormatInfo.GetFormat(formatType);
    }
}
