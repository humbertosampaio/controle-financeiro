using ControleFinanceiro.Domain.Contas;
using System.Globalization;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Customizations.Domain.Shared;

internal sealed class SufixoContaCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((int numero) =>
            (SufixoConta)(numero % 10_000).ToString("D4", CultureInfo.InvariantCulture));
    }
}
