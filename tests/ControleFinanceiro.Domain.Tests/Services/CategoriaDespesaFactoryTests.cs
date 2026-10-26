using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Services;

public sealed class CategoriaDespesaFactoryTests
{
    [Theory, DomainAutoData]
    public void CriarOutros_Deve_CriarACategoriaOutrosDoUsuarioSemAliases(IdUsuario idUsuario)
    {
        CategoriaDespesa outros = CategoriaDespesaFactory.CriarOutros(idUsuario);

        outros.IdUsuario.Should().Be(idUsuario);
        outros.Nome.Should().Be(CategoriaDespesa.NomeOutros);
        outros.Aliases.Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public void Resolver_Quando_CategoriaNovaComAlias_Deve_CriarACategoriaDoUsuarioComOAlias(
        IdUsuario idUsuario,
        TextoObrigatorio nome,
        Alias alias)
    {
        Result<IReadOnlyList<CategoriaDespesa>, Erro> result =
            CategoriaDespesaFactory.Resolver(idUsuario, categoriasDoUsuario: [], [new CategoriaNova(nome, alias)]);

        CategoriaDespesa categoria = result.Should().Succeed().Which.Should().ContainSingle().Subject;
        categoria.IdUsuario.Should().Be(idUsuario);
        categoria.Nome.Should().Be(nome);
        AliasCategoria aliasCategoria = categoria.Aliases.Should().ContainSingle().Subject;
        aliasCategoria.Alias.Should().Be(alias);
        aliasCategoria.IdUsuario.Should().Be(idUsuario);
    }

    [Theory, DomainAutoData]
    public void Resolver_Quando_CategoriaNovaSemAlias_Deve_CriarACategoriaSemAliases(IdUsuario idUsuario, TextoObrigatorio nome)
    {
        Result<IReadOnlyList<CategoriaDespesa>, Erro> result =
            CategoriaDespesaFactory.Resolver(idUsuario, categoriasDoUsuario: [], [new CategoriaNova(nome, alias: null)]);

        result.Should().Succeed().Which.Should().ContainSingle().Which.Aliases.Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public void Resolver_Quando_AliasEmCategoriaExistente_Deve_AdicionarOAliasSemCriarCategoria(
        [Frozen] IdUsuario idUsuario,
        CategoriaDespesa existente,
        Alias alias)
    {
        Result<IReadOnlyList<CategoriaDespesa>, Erro> result =
            CategoriaDespesaFactory.Resolver(idUsuario, [existente], [new AliasCategoriaExistente(existente, alias)]);

        result.Should().Succeed().Which.Should().BeEmpty();
        existente.Aliases.Should().Contain(a => a.Alias == alias && a.IdUsuario == idUsuario);
    }

    [Theory, DomainAutoData]
    public void Resolver_Quando_CategoriaJaPossuiOAlias_Deve_ManterUmAliasSo(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Alias alias,
        CategoriaDespesa existente)
    {
        Result<IReadOnlyList<CategoriaDespesa>, Erro> result =
            CategoriaDespesaFactory.Resolver(idUsuario, [existente], [new AliasCategoriaExistente(existente, alias)]);

        result.Should().Succeed();
        existente.Aliases.Should().ContainSingle();
    }

    [Theory, DomainAutoData]
    public void Resolver_Quando_LoteMisto_Deve_AplicarTodosOsItens(
        [Frozen] IdUsuario idUsuario,
        CategoriaDespesa existente,
        Alias aliasDaExistente,
        TextoObrigatorio nomeComAlias,
        Alias aliasDaNova,
        TextoObrigatorio nomeSemAlias)
    {
        Result<IReadOnlyList<CategoriaDespesa>, Erro> result = CategoriaDespesaFactory.Resolver(
            idUsuario,
            [existente],
            [
                new AliasCategoriaExistente(existente, aliasDaExistente),
                new CategoriaNova(nomeComAlias, aliasDaNova),
                new CategoriaNova(nomeSemAlias, alias: null),
            ]);

        IReadOnlyList<CategoriaDespesa> novas = result.Should().Succeed().Subject;
        novas.Select(c => c.Nome).Should().Equal(nomeComAlias, nomeSemAlias);
        novas[0].PossuiAlias(aliasDaNova).Should().BeTrue();
        novas[1].Aliases.Should().BeEmpty();
        existente.PossuiAlias(aliasDaExistente).Should().BeTrue();
    }

    [Theory, DomainAutoData]
    public void Resolver_Quando_NomeDeCategoriaExistenteComOutraCaixa_Deve_RetornarFalha(
        [Frozen] IdUsuario idUsuario,
        CategoriaDespesa existente)
    {
        TextoObrigatorio mesmoNome = (TextoObrigatorio)existente.Nome.Value.ToLowerInvariant();

        Result<IReadOnlyList<CategoriaDespesa>, Erro> result =
            CategoriaDespesaFactory.Resolver(idUsuario, [existente], [new CategoriaNova(mesmoNome, alias: null)]);

        result.Should().Fail()
            .Which.Should().BeOfType<ErroCategoriaJaCadastrada>()
            .Which.IdCategoria.Should().Be(existente.Id);
    }

    [Theory, DomainAutoData]
    public void Resolver_Quando_DuasCategoriasNovasComOMesmoNome_Deve_RetornarFalha(IdUsuario idUsuario, TextoObrigatorio nome)
    {
        Result<IReadOnlyList<CategoriaDespesa>, Erro> result = CategoriaDespesaFactory.Resolver(
            idUsuario,
            categoriasDoUsuario: [],
            [new CategoriaNova(nome, alias: null), new CategoriaNova(nome, alias: null)]);

        result.Should().Fail().Which.Should().BeOfType<ErroCategoriaJaCadastrada>();
    }

    [Theory, DomainAutoData]
    public void Resolver_Quando_NomeOutros_Deve_RetornarFalha([Frozen] IdUsuario idUsuario)
    {
        CategoriaDespesa outros = CategoriaDespesaFactory.CriarOutros(idUsuario);

        Result<IReadOnlyList<CategoriaDespesa>, Erro> result = CategoriaDespesaFactory.Resolver(
            idUsuario,
            [outros],
            [new CategoriaNova(CategoriaDespesa.NomeOutros, alias: null)]);

        result.Should().Fail()
            .Which.Should().BeOfType<ErroCategoriaJaCadastrada>()
            .Which.IdCategoria.Should().Be(outros.Id);
    }

    [Theory, DomainAutoData]
    public void Resolver_Quando_AliasDeOutraCategoriaDoUsuario_Deve_RetornarFalhaSemAlterarNada(
        [Frozen] IdUsuario idUsuario,
        CategoriaDespesa dono,
        TextoObrigatorio nome,
        Alias aliasLivre)
    {
        CategoriaDespesa destino = CategoriaDespesaFactory
            .Resolver(idUsuario, [dono], [new CategoriaNova(nome, alias: null)])
            .Value[0];
        Alias aliasDoDono = dono.Aliases[0].Alias;

        Result<IReadOnlyList<CategoriaDespesa>, Erro> result = CategoriaDespesaFactory.Resolver(
            idUsuario,
            [dono, destino],
            [new AliasCategoriaExistente(destino, aliasLivre), new AliasCategoriaExistente(destino, aliasDoDono)]);

        result.Should().Fail()
            .Which.Should().BeOfType<ErroAliasCategoriaJaAtribuido>()
            .Which.IdCategoria.Should().Be(dono.Id);
        destino.Aliases.Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public void Resolver_Quando_MesmoAliasParaDoisDestinosNoLote_Deve_RetornarFalha(
        IdUsuario idUsuario,
        TextoObrigatorio nome,
        TextoObrigatorio outroNome,
        Alias alias)
    {
        Result<IReadOnlyList<CategoriaDespesa>, Erro> result = CategoriaDespesaFactory.Resolver(
            idUsuario,
            categoriasDoUsuario: [],
            [new CategoriaNova(nome, alias), new CategoriaNova(outroNome, alias)]);

        result.Should().Fail().Which.Should().BeOfType<ErroAliasCategoriaJaAtribuido>();
    }

    [Theory, DomainAutoData]
    public void Resolver_Quando_ListaComCategoriaDeOutroUsuario_Deve_LancarExcecao(
        IdUsuario idUsuario,
        CategoriaDespesa categoriaDeOutroUsuario,
        TextoObrigatorio nome)
    {
        Action act = () => CategoriaDespesaFactory.Resolver(
            idUsuario,
            [categoriaDeOutroUsuario],
            [new CategoriaNova(nome, alias: null)]);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("categoriasDoUsuario");
    }

    [Theory, DomainAutoData]
    public void Resolver_Quando_ItemComCategoriaForaDaLista_Deve_LancarExcecao(
        [Frozen] IdUsuario idUsuario,
        CategoriaDespesa foraDaLista,
        Alias alias)
    {
        Action act = () => CategoriaDespesaFactory.Resolver(
            idUsuario,
            categoriasDoUsuario: [],
            [new AliasCategoriaExistente(foraDaLista, alias)]);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("itens");
    }

    [Theory, DomainAutoData]
    public void Renomear_Quando_NomeLivre_Deve_TrocarONomeSemMudarOsAliases(CategoriaDespesa categoria, TextoObrigatorio nome)
    {
        IReadOnlyList<AliasCategoria> antes = [.. categoria.Aliases];

        UnitResult<Erro> result = CategoriaDespesaFactory.Renomear([categoria], categoria, nome);

        result.Should().Succeed();
        categoria.Nome.Should().Be(nome);
        categoria.Aliases.Should().Equal(antes);
    }

    [Theory, DomainAutoData]
    public void Renomear_Quando_ProprioNomeComOutraCaixa_Deve_TrocarONome(CategoriaDespesa categoria)
    {
        TextoObrigatorio outraCaixa = (TextoObrigatorio)categoria.Nome.Value.ToLowerInvariant();

        UnitResult<Erro> result = CategoriaDespesaFactory.Renomear([categoria], categoria, outraCaixa);

        result.Should().Succeed();
        categoria.Nome.Should().Be(outraCaixa);
    }

    [Theory, DomainAutoData]
    public void Renomear_Quando_NomeDeOutraCategoria_Deve_RetornarFalhaSemRenomear(
        [Frozen] IdUsuario idUsuario,
        CategoriaDespesa outra,
        TextoObrigatorio nome)
    {
        CategoriaDespesa categoria = CategoriaDespesaFactory
            .Resolver(idUsuario, [outra], [new CategoriaNova(nome, alias: null)])
            .Value[0];

        UnitResult<Erro> result = CategoriaDespesaFactory.Renomear([outra, categoria], categoria, outra.Nome);

        result.Should().Fail()
            .Which.Should().BeOfType<ErroCategoriaJaCadastrada>()
            .Which.IdCategoria.Should().Be(outra.Id);
        categoria.Nome.Should().Be(nome);
    }

    [Theory, DomainAutoData]
    public void Renomear_Quando_CategoriaOutros_Deve_RetornarFalhaSemRenomear(IdUsuario idUsuario, TextoObrigatorio nome)
    {
        CategoriaDespesa outros = CategoriaDespesaFactory.CriarOutros(idUsuario);

        UnitResult<Erro> result = CategoriaDespesaFactory.Renomear([outros], outros, nome);

        result.Should().Fail().Which.Should().Be(ErroCategoriaOutrosNaoRenomeavel.Instancia);
        outros.Nome.Should().Be(CategoriaDespesa.NomeOutros);
    }

    [Theory, DomainAutoData]
    public void Renomear_Quando_ListaComCategoriaDeOutroUsuario_Deve_LancarExcecao(
        CategoriaDespesa categoria,
        CategoriaDespesa categoriaDeOutroUsuario,
        TextoObrigatorio nome)
    {
        Action act = () => CategoriaDespesaFactory.Renomear([categoria, categoriaDeOutroUsuario], categoria, nome);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("categoriasDoUsuario");
    }
}
