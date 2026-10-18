namespace ControleFinanceiro.Tests.Shared.AutoFixture.Customizations;

/// <summary>
/// Uma customização que permite gerar valores decimais com uma parte fracionária
/// </summary>
public sealed class StrictDecimalCustomization : ICustomization
{
    public void Customize(IFixture fixture) =>
        new StrictFloatingPointCustomization<decimal>().Customize(fixture);
}
