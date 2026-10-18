using ControleFinanceiro.Tests.Shared.AutoFixture.Specifications;
using ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Customizations;

internal sealed class VogenCustomization : ICustomization
{
    public void Customize(IFixture fixture) =>
        fixture.Customizations.Add(new VogenSpecimenBuilder(new VogenSpecification()));
}
