using AutoFixture.Xunit2;

namespace ControleFinanceiro.WebApi.Tests.AutoFixture;

public sealed class InlineApiAutoDataAttribute : InlineAutoDataAttribute
{
    public InlineApiAutoDataAttribute(params object?[]? values)
        : base(new ApiAutoDataAttribute(), values)
    {
    }
}
