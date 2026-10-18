using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Customizations.Domain.Shared;

public sealed class TextoObrigatorioCustomization : ICustomization
{
    public void Customize(IFixture fixture) => fixture.Register((string str) => (TextoObrigatorio)str);
}
