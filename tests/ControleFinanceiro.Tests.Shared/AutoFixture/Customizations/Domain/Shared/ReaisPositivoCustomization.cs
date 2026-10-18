using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Customizations.Domain.Shared;

internal sealed class ReaisPositivoCustomization : ICustomization
{
    public void Customize(IFixture fixture) =>
        fixture.Register((decimal valor) => (ReaisPositivo)valor);
}
