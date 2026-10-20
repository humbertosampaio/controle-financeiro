using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Customizations.Domain.Shared;

/// <summary>
/// Identidade única, pelo id externo: cada uma gerada é diferente das outras. Para uma identidade composta,
/// use a do <see cref="LancamentoExtrato"/> do fixture.
/// </summary>
internal sealed class IdentidadeLancamentoCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((string idExterno) => IdentidadeLancamento.Unica(idExterno).Value);
    }
}
