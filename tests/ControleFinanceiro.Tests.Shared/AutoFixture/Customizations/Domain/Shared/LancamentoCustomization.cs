using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Customizations.Domain.Shared;

internal sealed class LancamentoCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Inject(TipoOrigem.Conta);
        fixture.Customizations.Add(new LancamentoGenerator());
    }
}
