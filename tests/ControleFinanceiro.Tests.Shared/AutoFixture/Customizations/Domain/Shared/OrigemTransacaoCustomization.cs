using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Customizations.Domain.Shared;

internal sealed class OrigemTransacaoCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((ContaBancaria conta) => OrigemTransacao.DaConta(conta.Id));
    }
}
