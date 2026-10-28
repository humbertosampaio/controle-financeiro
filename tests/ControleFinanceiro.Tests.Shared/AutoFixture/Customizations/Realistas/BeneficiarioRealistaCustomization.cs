using Bogus.DataSets;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Customizations.Realistas;

/// <summary>
/// Beneficiário com nome completo em pt-BR e, como alias, o mesmo nome em maiúsculas, como o extrato costuma trazer a
/// contraparte. Os acentos ficam no alias, para o texto original e o normalizado chegarem diferentes ao Postgres. Com
/// <c>[Frozen] IdUsuario</c> no teste, o beneficiário sai desse usuário.
/// </summary>
public sealed class BeneficiarioRealistaCustomization : ICustomization
{
    private readonly Name _nomes;

    public BeneficiarioRealistaCustomization(Name nomes)
    {
        _nomes = nomes;
    }

    public void Customize(IFixture fixture)
    {
        fixture.Register((IdUsuario idUsuario) =>
        {
            string nome = _nomes.FullName();
            BeneficiarioNovo novo = new((TextoObrigatorio)nome, (Alias)nome.ToUpperInvariant());

            return BeneficiarioFactory.Resolver(idUsuario, beneficiariosDoUsuario: [], [novo]).Value[0];
        });
    }
}
