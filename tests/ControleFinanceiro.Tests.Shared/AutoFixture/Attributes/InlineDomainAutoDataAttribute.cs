using AutoFixture.Xunit2;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

public class InlineDomainAutoDataAttribute : InlineAutoDataAttribute
{
    public InlineDomainAutoDataAttribute(params object?[]? values)
        : base(new DomainAutoDataAttribute(), values)
    {
    }
}
