using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.WebApi.Endpoints.Beneficiarios.Renomear;

namespace ControleFinanceiro.WebApi.Tests.Beneficiarios.Renomear;

/// <summary>
/// Monta o <see cref="RenomearBeneficiarioRequest"/> com um <see cref="TextoObrigatorio"/> do fixture. Os ids ficam de
/// fora: vêm da rota, que vence o corpo.
/// </summary>
internal sealed class RenomearBeneficiarioRequestCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((TextoObrigatorio nome) => new RenomearBeneficiarioRequest { Nome = nome });
    }
}
