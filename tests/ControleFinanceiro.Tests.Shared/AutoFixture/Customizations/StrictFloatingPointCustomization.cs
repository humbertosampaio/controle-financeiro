using System.Numerics;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Customizations;

/// <summary>
/// Uma customização que permite gerar valores de ponto flutuante com parte fracionária.
/// </summary>
public sealed class StrictFloatingPointCustomization<T> : ICustomization
    where T : struct, IFloatingPoint<T>
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<T>(c =>
            c.FromFactory((int inteiro, int fracional) =>
            {
                double fractional = (double)fracional % 100 / 100.0;
                return T.CreateTruncating(inteiro) + T.CreateTruncating(fractional);
            }));
    }
}
