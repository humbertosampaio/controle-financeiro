using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Beneficiarios;

public sealed class BeneficiarioTests
{
    [Theory, DomainAutoData]
    public void PossuiAlias_Quando_MesmoTextoNormalizado_Deve_RetornarVerdadeiro([Frozen] Alias alias, Beneficiario beneficiario)
    {
        string desnormalizado = $"  {alias.TextoOriginal.ToLowerInvariant()} ";

        bool result = beneficiario.PossuiAlias((Alias)desnormalizado);

        result.Should().BeTrue();
    }

    [Theory, DomainAutoData]
    public void PossuiAlias_Quando_AliasDeOutroTexto_Deve_RetornarFalso(Beneficiario beneficiario, Alias outroAlias)
    {
        bool result = beneficiario.PossuiAlias(outroAlias);

        result.Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void RemoverAlias_Quando_PossuiOAlias_Deve_Remover([Frozen] Alias alias, Beneficiario beneficiario)
    {
        beneficiario.RemoverAlias(alias);

        beneficiario.Aliases.Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public void RemoverAlias_Quando_NaoPossuiOAlias_Deve_ManterOsAliases(Beneficiario beneficiario, Alias outroAlias)
    {
        IReadOnlyList<AliasBeneficiario> antes = [.. beneficiario.Aliases];

        beneficiario.RemoverAlias(outroAlias);

        beneficiario.Aliases.Should().Equal(antes);
    }

    [Theory, DomainAutoData]
    public void Renomear_Deve_TrocarONomeSemMudarOsAliases(Beneficiario beneficiario, TextoObrigatorio nome)
    {
        IReadOnlyList<AliasBeneficiario> antes = [.. beneficiario.Aliases];

        beneficiario.Renomear(nome);

        beneficiario.Nome.Should().Be(nome);
        beneficiario.Aliases.Should().Equal(antes);
    }
}
