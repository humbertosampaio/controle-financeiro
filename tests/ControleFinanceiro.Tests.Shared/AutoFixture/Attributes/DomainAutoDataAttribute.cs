using AutoFixture;
using AutoFixture.Xunit2;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

public class DomainAutoDataAttribute : AutoDataAttribute
{
    public DomainAutoDataAttribute() : base(DomainFixture.Create)
    {
    }

    public DomainAutoDataAttribute(Action<IFixture> config)
        : base(() =>
        {
            IFixture fixture = DomainFixture.Create();
            config(fixture);
            return fixture;
        })
    {
    }
}