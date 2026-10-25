using ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Customizations.Domain.Shared;

internal sealed class CnpjCustomization : ICustomization
{
    public void Customize(IFixture fixture) =>
        fixture.Customizations.Add(new CnpjGenerator());
}
