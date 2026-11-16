using ControleFinanceiro.Domain.Cartoes;
using System.Globalization;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Customizations.Domain.Shared;

internal sealed class SufixoCartaoCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((int numero) =>
            (SufixoCartao)(numero % 10_000).ToString("D4", CultureInfo.InvariantCulture));
    }
}
