using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Services;

public sealed class BeneficiarioFactoryTests
{
    [Theory, DomainAutoData]
    public void Criar_Quando_ComAlias_Deve_CriarOBeneficiarioDoUsuarioComOAlias(
        IdUsuario idUsuario,
        TextoObrigatorio nome,
        Alias alias)
    {
        Result<Beneficiario, Erro> result = BeneficiarioFactory.Criar(idUsuario, beneficiariosDoUsuario: [], nome, alias);

        Beneficiario beneficiario = result.Should().Succeed().Subject;
        beneficiario.IdUsuario.Should().Be(idUsuario);
        beneficiario.Nome.Should().Be(nome);
        AliasBeneficiario aliasBeneficiario = beneficiario.Aliases.Should().ContainSingle().Subject;
        aliasBeneficiario.Alias.Should().Be(alias);
        aliasBeneficiario.IdUsuario.Should().Be(idUsuario);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_SemAlias_Deve_CriarOBeneficiarioSemAliases(IdUsuario idUsuario, TextoObrigatorio nome)
    {
        Result<Beneficiario, Erro> result = BeneficiarioFactory.Criar(idUsuario, beneficiariosDoUsuario: [], nome, alias: null);

        result.Should().Succeed().Which.Aliases.Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_AliasDeOutroBeneficiarioDoUsuario_Deve_RetornarFalha(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Alias alias,
        Beneficiario existente,
        TextoObrigatorio nome)
    {
        Result<Beneficiario, Erro> result = BeneficiarioFactory.Criar(idUsuario, [existente], nome, alias);

        result.Should().Fail()
            .Which.Should().BeOfType<ErroAliasBeneficiarioJaAtribuido>()
            .Which.IdBeneficiario.Should().Be(existente.Id);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_SemAliasEComBeneficiariosDoUsuario_Deve_CriarOBeneficiario(
        [Frozen] IdUsuario idUsuario,
        Beneficiario existente,
        TextoObrigatorio nome)
    {
        Result<Beneficiario, Erro> result = BeneficiarioFactory.Criar(idUsuario, [existente], nome, alias: null);

        result.Should().Succeed().Which.Should().NotBe(existente);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_ListaComBeneficiarioDeOutroUsuario_Deve_LancarExcecao(
        IdUsuario idUsuario,
        Beneficiario beneficiarioDeOutroUsuario,
        TextoObrigatorio nome,
        Alias alias)
    {
        Action act = () => BeneficiarioFactory.Criar(idUsuario, [beneficiarioDeOutroUsuario], nome, alias);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("beneficiariosDoUsuario");
    }

    [Theory, DomainAutoData]
    public void AtribuirAlias_Quando_AliasLivre_Deve_AdicionarOAliasAoBeneficiario(
        [Frozen] IdUsuario idUsuario,
        Beneficiario beneficiario,
        Beneficiario outro,
        Alias alias)
    {
        UnitResult<Erro> result = BeneficiarioFactory.AtribuirAlias([beneficiario, outro], beneficiario, alias);

        result.Should().Succeed();
        beneficiario.PossuiAlias(alias).Should().BeTrue();
        beneficiario.Aliases.Should().Contain(a => a.Alias == alias && a.IdUsuario == idUsuario);
    }

    [Theory, DomainAutoData]
    public void AtribuirAlias_Quando_BeneficiarioJaPossuiOAlias_Deve_ManterUmAliasSo([Frozen] Alias alias, Beneficiario beneficiario)
    {
        UnitResult<Erro> result = BeneficiarioFactory.AtribuirAlias([beneficiario], beneficiario, alias);

        result.Should().Succeed();
        beneficiario.Aliases.Should().ContainSingle();
    }

    [Theory, DomainAutoData]
    public void AtribuirAlias_Quando_AliasDeOutroBeneficiarioDoUsuario_Deve_RetornarFalhaSemAdicionar(
        [Frozen] IdUsuario idUsuario,
        Beneficiario dono,
        TextoObrigatorio nome)
    {
        Beneficiario beneficiario = BeneficiarioFactory.Criar(idUsuario, [dono], nome, alias: null).Value;
        Alias aliasDoDono = dono.Aliases[0].Alias;

        UnitResult<Erro> result = BeneficiarioFactory.AtribuirAlias([dono, beneficiario], beneficiario, aliasDoDono);

        result.Should().Fail()
            .Which.Should().BeOfType<ErroAliasBeneficiarioJaAtribuido>()
            .Which.IdBeneficiario.Should().Be(dono.Id);
        beneficiario.PossuiAlias(aliasDoDono).Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void AtribuirAlias_Quando_ListaComBeneficiarioDeOutroUsuario_Deve_LancarExcecao(
        Beneficiario beneficiario,
        Beneficiario beneficiarioDeOutroUsuario,
        Alias alias)
    {
        Action act = () => BeneficiarioFactory.AtribuirAlias([beneficiario, beneficiarioDeOutroUsuario], beneficiario, alias);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("beneficiariosDoUsuario");
    }
}
