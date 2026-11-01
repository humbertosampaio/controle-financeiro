using AutoFixture;
using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Services;

public sealed class AliasesCategoriaPendentesTests
{
    [Theory, DomainAutoData]
    public void Listar_Quando_SemCategorias_Deve_DevolverTodosOsTextos(
        IdUsuario idUsuario,
        Lancamento primeiro,
        Lancamento segundo)
    {
        IReadOnlyList<Alias> result = AliasesCategoriaPendentes.Listar(idUsuario, categoriasDoUsuario: [], [primeiro, segundo]);

        result.Select(a => a.TextoOriginal).Should().Equal(primeiro.CategoriaBanco, segundo.CategoriaBanco);
    }

    [Theory, DomainAutoData]
    public void Listar_Quando_AliasEmOutraCaixaComAcentoOuEspacosRepetidos_Deve_OmitirOTexto(
        [Frozen] IdUsuario idUsuario,
        Lancamento lancamento,
        Fixture fixture)
    {
        fixture.Inject((Alias)"Supermercado São João");
        CategoriaDespesa categoria = fixture.Create<CategoriaDespesa>();

        IReadOnlyList<Alias> result = AliasesCategoriaPendentes.Listar(
            idUsuario,
            [categoria],
            [ComCategoria(lancamento, "SUPERMERCADO  sao joao")]);

        result.Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public void Listar_Quando_GrafiasDiferentesDoMesmoTexto_Deve_DevolverUmComOOriginalDaPrimeira(
        IdUsuario idUsuario,
        Lancamento lancamento)
    {
        Lancamento[] lancamentos = [ComCategoria(lancamento, "Restaurante São João"), ComCategoria(lancamento, "RESTAURANTE  SAO JOAO")];

        IReadOnlyList<Alias> result = AliasesCategoriaPendentes.Listar(idUsuario, categoriasDoUsuario: [], lancamentos);

        result.Should().ContainSingle().Which.TextoOriginal.Should().Be("Restaurante São João");
    }

    [Theory, DomainAutoData]
    public void Listar_Quando_VariosTextos_Deve_ManterAOrdemDaPrimeiraOcorrencia(
        IdUsuario idUsuario,
        Lancamento lancamento,
        string primeira,
        string segunda,
        string terceira)
    {
        Lancamento[] lancamentos =
        [
            ComCategoria(lancamento, primeira),
            ComCategoria(lancamento, segunda),
            ComCategoria(lancamento, primeira),
            ComCategoria(lancamento, terceira),
        ];

        IReadOnlyList<Alias> result = AliasesCategoriaPendentes.Listar(idUsuario, categoriasDoUsuario: [], lancamentos);

        result.Select(a => a.TextoOriginal).Should().Equal(primeira, segunda, terceira);
    }

    [Theory, DomainAutoData]
    public void Listar_Quando_CategoriaBancoNula_Deve_Ignorar(IdUsuario idUsuario, Lancamento lancamento)
    {
        IReadOnlyList<Alias> result = AliasesCategoriaPendentes.Listar(
            idUsuario,
            categoriasDoUsuario: [],
            [ComCategoria(lancamento, categoriaBanco: null)]);

        result.Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public void Listar_Quando_TextoNaoViraAlias_Deve_Ignorar(IdUsuario idUsuario, Lancamento lancamento)
    {
        IReadOnlyList<Alias> result = AliasesCategoriaPendentes.Listar(
            idUsuario,
            categoriasDoUsuario: [],
            [ComCategoria(lancamento, "́́")]);

        result.Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public void Listar_Quando_LancamentoDeEntrada_Deve_DevolverACategoria(
        IdUsuario idUsuario,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        IdentidadeLancamento identidade,
        string categoriaBanco)
    {
        Lancamento estorno = Lancamento.DaConta(data, hora: null, valor, descricao, contraparte: null, categoriaBanco, identidade);

        IReadOnlyList<Alias> result = AliasesCategoriaPendentes.Listar(idUsuario, categoriasDoUsuario: [], [estorno]);

        result.Should().ContainSingle().Which.TextoOriginal.Should().Be(categoriaBanco);
    }

    [Theory, DomainAutoData]
    public void Listar_Quando_ListaComCategoriaDeOutroUsuario_Deve_LancarExcecao(
        IdUsuario idUsuario,
        CategoriaDespesa deOutroUsuario,
        Lancamento lancamento)
    {
        Action listar = () => AliasesCategoriaPendentes.Listar(idUsuario, [deOutroUsuario], [lancamento]);

        listar.Should().ThrowExactly<ArgumentException>().WithParameterName("categoriasDoUsuario");
    }

    [Theory, DomainAutoData]
    public void Conferir_Quando_TodosOsTextosSaoAlias_Deve_RetornarSucesso(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Alias alias,
        CategoriaDespesa categoria,
        Lancamento lancamento)
    {
        UnitResult<Erro> result = AliasesCategoriaPendentes.Conferir(
            idUsuario,
            [categoria],
            [ComCategoria(lancamento, alias.TextoOriginal), ComCategoria(lancamento, categoriaBanco: null)]);

        result.Should().Succeed();
    }

    [Theory, DomainAutoData]
    public void Conferir_Quando_TextoSemAlias_Deve_RetornarCategoriasBancoNaoResolvidas(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Alias alias,
        CategoriaDespesa categoria,
        Lancamento lancamento,
        string semAlias)
    {
        UnitResult<Erro> result = AliasesCategoriaPendentes.Conferir(
            idUsuario,
            [categoria],
            [ComCategoria(lancamento, alias.TextoOriginal), ComCategoria(lancamento, semAlias)]);

        result.Should().Fail().Which.Should().Be(ErroCategoriasBancoNaoResolvidas.Instancia);
    }

    private static Lancamento ComCategoria(Lancamento lancamento, string? categoriaBanco)
    {
        return Lancamento.DaConta(
            lancamento.Data,
            lancamento.Hora,
            lancamento.Valor,
            lancamento.Descricao,
            lancamento.Contraparte,
            categoriaBanco,
            lancamento.Identidade);
    }
}
