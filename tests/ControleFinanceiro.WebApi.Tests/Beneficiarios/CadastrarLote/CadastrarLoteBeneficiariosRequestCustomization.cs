using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.WebApi.Endpoints.Beneficiarios.CadastrarLote;

namespace ControleFinanceiro.WebApi.Tests.Beneficiarios.CadastrarLote;

/// <summary>
/// Monta o <see cref="ItemBeneficiarioDto"/> como um beneficiário novo a partir de um <see cref="Beneficiario"/> do
/// fixture, com o nome e o alias dele, e o <see cref="CadastrarLoteBeneficiariosRequest"/> com alguns desses itens, de
/// aliases distintos. O <see cref="CadastrarLoteBeneficiariosRequest.IdUsuario"/> fica de fora: vem da rota, que vence
/// o corpo.
/// </summary>
internal sealed class CadastrarLoteBeneficiariosRequestCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((Beneficiario beneficiario) => new ItemBeneficiarioDto
        {
            Nome = beneficiario.Nome,
            Alias = beneficiario.Aliases[0].Alias.TextoOriginal,
        });

        fixture.Register((ItemBeneficiarioDto[] itens) => new CadastrarLoteBeneficiariosRequest { Itens = itens });
    }
}
