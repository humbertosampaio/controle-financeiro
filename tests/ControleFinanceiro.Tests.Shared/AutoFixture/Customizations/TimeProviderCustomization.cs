using AutoFixture;
using Microsoft.Extensions.Time.Testing;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Customizations;

internal sealed class TimeProviderCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        FakeTimeProvider fakeTimeProvider = new();
        fakeTimeProvider.SetLocalTimeZone(TimeZoneInfo.Local);

        fixture.Inject(fakeTimeProvider);
        fixture.Inject<TimeProvider>(fakeTimeProvider);
    }
}
