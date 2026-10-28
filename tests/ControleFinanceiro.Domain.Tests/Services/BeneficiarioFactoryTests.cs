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
    public void Resolver_Quando_BeneficiarioNovoComAlias_Deve_CriarOBeneficiarioDoUsuarioComOAlias(
        IdUsuario idUsuario,
        TextoObrigatorio nome,
        Alias alias)
    {
        Result<IReadOnlyList<Beneficiario>, Erro> result =
            BeneficiarioFactory.Resolver(idUsuario, beneficiariosDoUsuario: [], [new BeneficiarioNovo(nome, alias)]);

        Beneficiario beneficiario = result.Should().Succeed().Which.Should().ContainSingle().Subject;
        beneficiario.IdUsuario.Should().Be(idUsuario);
        beneficiario.Nome.Should().Be(nome);
        AliasBeneficiario aliasBeneficiario = beneficiario.Aliases.Should().ContainSingle().Subject;
        aliasBeneficiario.Alias.Should().Be(alias);
        aliasBeneficiario.IdUsuario.Should().Be(idUsuario);
    }

    [Theory, DomainAutoData]
    public void Resolver_Quando_BeneficiarioNovoSemAlias_Deve_CriarOBeneficiarioSemAliases(IdUsuario idUsuario, TextoObrigatorio nome)
    {
        Result<IReadOnlyList<Beneficiario>, Erro> result =
            BeneficiarioFactory.Resolver(idUsuario, beneficiariosDoUsuario: [], [new BeneficiarioNovo(nome, alias: null)]);

        result.Should().Succeed().Which.Should().ContainSingle().Which.Aliases.Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public void Resolver_Quando_AliasEmBeneficiarioExistente_Deve_AdicionarOAliasSemCriarBeneficiario(
        [Frozen] IdUsuario idUsuario,
        Beneficiario existente,
        Alias alias)
    {
        Result<IReadOnlyList<Beneficiario>, Erro> result =
            BeneficiarioFactory.Resolver(idUsuario, [existente], [new AliasBeneficiarioExistente(existente, alias)]);

        result.Should().Succeed().Which.Should().BeEmpty();
        existente.Aliases.Should().Contain(a => a.Alias == alias && a.IdUsuario == idUsuario);
    }

    [Theory, DomainAutoData]
    public void Resolver_Quando_BeneficiarioJaPossuiOAlias_Deve_ManterUmAliasSo(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Alias alias,
        Beneficiario existente)
    {
        Result<IReadOnlyList<Beneficiario>, Erro> result =
            BeneficiarioFactory.Resolver(idUsuario, [existente], [new AliasBeneficiarioExistente(existente, alias)]);

        result.Should().Succeed();
        existente.Aliases.Should().ContainSingle();
    }

    [Theory, DomainAutoData]
    public void Resolver_Quando_LoteMisto_Deve_AplicarTodosOsItens(
        [Frozen] IdUsuario idUsuario,
        Beneficiario existente,
        Alias aliasDoExistente,
        TextoObrigatorio nomeComAlias,
        Alias aliasDoNovo,
        TextoObrigatorio nomeSemAlias)
    {
        Result<IReadOnlyList<Beneficiario>, Erro> result = BeneficiarioFactory.Resolver(
            idUsuario,
            [existente],
            [
                new AliasBeneficiarioExistente(existente, aliasDoExistente),
                new BeneficiarioNovo(nomeComAlias, aliasDoNovo),
                new BeneficiarioNovo(nomeSemAlias, alias: null),
            ]);

        IReadOnlyList<Beneficiario> novos = result.Should().Succeed().Subject;
        novos.Select(b => b.Nome).Should().Equal(nomeComAlias, nomeSemAlias);
        novos[0].PossuiAlias(aliasDoNovo).Should().BeTrue();
        novos[1].Aliases.Should().BeEmpty();
        existente.PossuiAlias(aliasDoExistente).Should().BeTrue();
    }

    [Theory, DomainAutoData]
    public void Resolver_Quando_DoisNovosComOMesmoNome_Deve_CriarOsDois([Frozen] IdUsuario idUsuario, Beneficiario existente)
    {
        Result<IReadOnlyList<Beneficiario>, Erro> result = BeneficiarioFactory.Resolver(
            idUsuario,
            [existente],
            [new BeneficiarioNovo(existente.Nome, alias: null), new BeneficiarioNovo(existente.Nome, alias: null)]);

        IReadOnlyList<Beneficiario> novos = result.Should().Succeed().Subject;
        novos.Should().HaveCount(2).And.OnlyContain(b => b.Nome == existente.Nome);
        novos.Select(b => b.Id).Should().OnlyHaveUniqueItems().And.NotContain(existente.Id);
    }

    [Theory, DomainAutoData]
    public void Resolver_Quando_AliasDeOutroBeneficiarioDoUsuario_Deve_RetornarFalhaSemAlterarNada(
        [Frozen] IdUsuario idUsuario,
        Beneficiario dono,
        TextoObrigatorio nome,
        Alias aliasLivre)
    {
        Beneficiario destino = BeneficiarioFactory
            .Resolver(idUsuario, [dono], [new BeneficiarioNovo(nome, alias: null)])
            .Value[0];
        Alias aliasDoDono = dono.Aliases[0].Alias;

        Result<IReadOnlyList<Beneficiario>, Erro> result = BeneficiarioFactory.Resolver(
            idUsuario,
            [dono, destino],
            [new AliasBeneficiarioExistente(destino, aliasLivre), new AliasBeneficiarioExistente(destino, aliasDoDono)]);

        result.Should().Fail()
            .Which.Should().BeOfType<ErroAliasBeneficiarioJaAtribuido>()
            .Which.IdBeneficiario.Should().Be(dono.Id);
        destino.Aliases.Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public void Resolver_Quando_MesmoAliasParaDoisDestinosNoLote_Deve_RetornarFalha(
        IdUsuario idUsuario,
        TextoObrigatorio nome,
        TextoObrigatorio outroNome,
        Alias alias)
    {
        Result<IReadOnlyList<Beneficiario>, Erro> result = BeneficiarioFactory.Resolver(
            idUsuario,
            beneficiariosDoUsuario: [],
            [new BeneficiarioNovo(nome, alias), new BeneficiarioNovo(outroNome, alias)]);

        result.Should().Fail().Which.Should().BeOfType<ErroAliasBeneficiarioJaAtribuido>();
    }

    [Theory, DomainAutoData]
    public void Resolver_Quando_MesmoAliasDuasVezesParaOMesmoDestino_Deve_ManterUmAliasSo(
        [Frozen] IdUsuario idUsuario,
        Beneficiario existente,
        Alias alias)
    {
        Result<IReadOnlyList<Beneficiario>, Erro> result = BeneficiarioFactory.Resolver(
            idUsuario,
            [existente],
            [new AliasBeneficiarioExistente(existente, alias), new AliasBeneficiarioExistente(existente, alias)]);

        result.Should().Succeed();
        existente.Aliases.Should().ContainSingle(a => a.Alias == alias);
    }

    [Theory, DomainAutoData]
    public void Resolver_Quando_ListaComBeneficiarioDeOutroUsuario_Deve_LancarExcecao(
        IdUsuario idUsuario,
        Beneficiario beneficiarioDeOutroUsuario,
        TextoObrigatorio nome)
    {
        Action act = () => BeneficiarioFactory.Resolver(
            idUsuario,
            [beneficiarioDeOutroUsuario],
            [new BeneficiarioNovo(nome, alias: null)]);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("beneficiariosDoUsuario");
    }

    [Theory, DomainAutoData]
    public void Resolver_Quando_ItemComBeneficiarioForaDaLista_Deve_LancarExcecao(
        [Frozen] IdUsuario idUsuario,
        Beneficiario foraDaLista,
        Alias alias)
    {
        Action act = () => BeneficiarioFactory.Resolver(
            idUsuario,
            beneficiariosDoUsuario: [],
            [new AliasBeneficiarioExistente(foraDaLista, alias)]);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("itens");
    }
}
