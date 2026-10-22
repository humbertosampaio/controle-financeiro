using ControleFinanceiro.Domain.Cartoes;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Customizations.Domain.Shared;

internal sealed class DiaVencimentoCartaoCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((int dia) =>
        {
            dia = dia % 28 + 1;
            return (DiaVencimentoCartao)dia;
        });
    }
}
