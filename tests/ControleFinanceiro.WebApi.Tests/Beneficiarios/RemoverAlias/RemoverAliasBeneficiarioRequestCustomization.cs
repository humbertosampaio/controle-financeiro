using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.WebApi.Endpoints.Beneficiarios.RemoverAlias;

namespace ControleFinanceiro.WebApi.Tests.Beneficiarios.RemoverAlias;

/// <summary>
/// Monta o <see cref="RemoverAliasBeneficiarioRequest"/> com um <see cref="Alias"/> do fixture, que nenhum beneficiário
/// tem. Os ids ficam de fora: vêm da rota, que vence o corpo.
/// </summary>
internal sealed class RemoverAliasBeneficiarioRequestCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((Alias alias) => new RemoverAliasBeneficiarioRequest { Alias = alias.TextoOriginal });
    }
}
