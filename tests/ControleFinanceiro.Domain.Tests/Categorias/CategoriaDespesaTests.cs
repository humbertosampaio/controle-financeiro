using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Categorias;

public sealed class CategoriaDespesaTests
{
    [Theory, DomainAutoData]
    public void PossuiAlias_Quando_MesmoTextoNormalizado_Deve_RetornarVerdadeiro([Frozen] Alias alias, CategoriaDespesa categoria)
    {
        string desnormalizado = $"  {alias.TextoOriginal.ToLowerInvariant()} ";

        bool result = categoria.PossuiAlias((Alias)desnormalizado);

        result.Should().BeTrue();
    }

    [Theory, DomainAutoData]
    public void PossuiAlias_Quando_AliasDeOutroTexto_Deve_RetornarFalso(CategoriaDespesa categoria, Alias outroAlias)
    {
        bool result = categoria.PossuiAlias(outroAlias);

        result.Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void RemoverAlias_Quando_PossuiOAlias_Deve_Remover([Frozen] Alias alias, CategoriaDespesa categoria)
    {
        categoria.RemoverAlias(alias);

        categoria.Aliases.Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public void RemoverAlias_Quando_NaoPossuiOAlias_Deve_ManterOsAliases(CategoriaDespesa categoria, Alias outroAlias)
    {
        IReadOnlyList<AliasCategoria> antes = [.. categoria.Aliases];

        categoria.RemoverAlias(outroAlias);

        categoria.Aliases.Should().Equal(antes);
    }

    [Theory]
    [InlineDomainAutoData("Alimentação", "  alimentacao ")]
    [InlineDomainAutoData("Saúde e  bem-estar", "SAUDE E BEM-ESTAR")]
    public void PossuiNome_Quando_MesmoNomeNormalizado_Deve_RetornarVerdadeiro(
        string nome,
        string desnormalizado,
        IdUsuario idUsuario)
    {
        CategoriaDespesa categoria = CategoriaDespesaFactory
            .Resolver(idUsuario, categoriasDoUsuario: [], [new CategoriaNova((TextoObrigatorio)nome, alias: null)])
            .Value[0];

        bool result = categoria.PossuiNome((TextoObrigatorio)desnormalizado);

        result.Should().BeTrue();
    }

    [Theory, DomainAutoData]
    public void PossuiNome_Quando_OutroNome_Deve_RetornarFalso(CategoriaDespesa categoria, TextoObrigatorio outroNome)
    {
        bool result = categoria.PossuiNome(outroNome);

        result.Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void EhOutros_Quando_CategoriaOutros_Deve_RetornarVerdadeiro(IdUsuario idUsuario)
    {
        CategoriaDespesa outros = CategoriaDespesaFactory.CriarOutros(idUsuario);

        outros.EhOutros.Should().BeTrue();
    }

    [Theory, DomainAutoData]
    public void EhOutros_Quando_OutraCategoria_Deve_RetornarFalso(CategoriaDespesa categoria)
    {
        categoria.EhOutros.Should().BeFalse();
    }
}
