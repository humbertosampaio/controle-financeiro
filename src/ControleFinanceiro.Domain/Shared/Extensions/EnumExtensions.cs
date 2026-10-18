using Ardalis.SmartEnum;

namespace ControleFinanceiro.Domain.Shared.Extensions;

public static class EnumExtensions
{
    extension<TEnum, TValue>(SmartEnum<TEnum, TValue>)
        where TEnum : SmartEnum<TEnum, TValue>
        where TValue : IEquatable<TValue>, IComparable<TValue>
    {
        public static Result<TEnum> TryFromValue(TValue value)
        {
            return SmartEnum<TEnum, TValue>.TryFromValue(value, out TEnum @enum)
                ? Result.Success(@enum)
                : Result.Failure<TEnum>($"Valor '{value}' não corresponde a um {typeof(TEnum).Name} válido");
        }
    }
}
