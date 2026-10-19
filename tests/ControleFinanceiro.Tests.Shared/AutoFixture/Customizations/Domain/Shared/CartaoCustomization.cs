using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Customizations.Domain.Shared;

internal sealed class CartaoCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((SufixoCartao sufixo, DiaVencimentoCartao diaVencimento) =>
            new Cartao(sufixo, diaVencimento));
    }
}
