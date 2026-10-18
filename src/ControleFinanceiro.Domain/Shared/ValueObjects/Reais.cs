using CSharpFunctionalExtensions;
using System.Globalization;

namespace ControleFinanceiro.Domain.Shared.ValueObjects;

/// <summary>
/// Representa um valor monetário em Reais (BRL)
/// </summary>
public class Reais(decimal valor) : SimpleValueObject<decimal>(valor)
{
    internal static readonly ReaisFormatProvider FormatProvider = new();

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
