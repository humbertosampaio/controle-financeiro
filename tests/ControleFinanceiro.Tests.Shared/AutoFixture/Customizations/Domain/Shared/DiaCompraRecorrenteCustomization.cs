using ControleFinanceiro.Domain.Despesas;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Customizations.Domain.Shared;

internal sealed class DiaCompraRecorrenteCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((int dia) =>
        {
            dia = dia % 28 + 1;
            return (DiaCompraRecorrente)dia;
        });
    }
}
