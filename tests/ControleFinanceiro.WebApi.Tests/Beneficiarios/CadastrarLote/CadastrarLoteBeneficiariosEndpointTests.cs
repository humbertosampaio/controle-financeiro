using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.WebApi.Endpoints.Beneficiarios.CadastrarLote;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;
using System.Net.Http.Json;

namespace ControleFinanceiro.WebApi.Tests.Beneficiarios.CadastrarLote;

[Collection(ApiCollection.Nome)]
public sealed class CadastrarLoteBeneficiariosEndpointTests : IAsyncLifetime
{
    private readonly ApiComPostgresFixture _api;

    public CadastrarLoteBeneficiariosEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory, ApiAutoData]
    public async Task Post_Quando_BeneficiariosNovos_Deve_GravarCadaUmComOAlias(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest requisicao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await CadastrarAsync(idUsuario, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        CadastrarLoteBeneficiariosResponse? corpo = await resposta.Content.ReadFromJsonAsync<CadastrarLoteBeneficiariosResponse>();
        List<Beneficiario> gravados = await LerBeneficiariosAsync();
        gravados.Should().OnlyContain(b => b.IdUsuario == IdUsuario.From(idUsuario));
        gravados.SelectMany(b => b.Aliases).Should().OnlyContain(a => a.IdUsuario == IdUsuario.From(idUsuario));
        gravados
            .Select(b => (
                b.Id,
                b.Nome,
                b.Aliases.Single().Alias.TextoOriginal,
                b.Aliases.Single().Alias.TextoNormalizado))
            .Should().BeEquivalentTo(requisicao.Itens.Select((item, i) => (
                IdBeneficiario.From(corpo!.IdsBeneficiarios[i]),
                (TextoObrigatorio)item.Nome,
                item.Alias,
                ((Alias)item.Alias).TextoNormalizado)));
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_AliasEmExistenteNovoComAliasENovoSemAlias_Deve_GravarOsTresItensNumCommit(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest anterior,
        Alias aliasDoExistente,
        ItemBeneficiarioDto novoComAlias,
        TextoObrigatorio nomeSemAlias)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        Guid idExistente = (await _api.CadastrarBeneficiariosAsync(idUsuario, anterior))[0];
        CadastrarLoteBeneficiariosRequest requisicao = new()
        {
            Itens =
            [
                new ItemBeneficiarioDto { IdBeneficiario = idExistente, Alias = aliasDoExistente.TextoOriginal },
                novoComAlias,
                new ItemBeneficiarioDto { Nome = nomeSemAlias },
            ],
        };

        using HttpResponseMessage resposta = await CadastrarAsync(idUsuario, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        CadastrarLoteBeneficiariosResponse? corpo = await resposta.Content.ReadFromJsonAsync<CadastrarLoteBeneficiariosResponse>();
        corpo!.IdsBeneficiarios[0].Should().Be(idExistente);
        List<Beneficiario> gravados = await LerBeneficiariosAsync();
        gravados.Single(b => b.Id == IdBeneficiario.From(idExistente)).Aliases.Select(a => a.Alias.TextoOriginal)
            .Should().Contain(aliasDoExistente.TextoOriginal);
        Beneficiario comAlias = gravados.Single(b => b.Id == IdBeneficiario.From(corpo.IdsBeneficiarios[1]));
        comAlias.Nome.Should().Be((TextoObrigatorio)novoComAlias.Nome);
        comAlias.Aliases.Should().ContainSingle().Which.Alias.TextoOriginal.Should().Be(novoComAlias.Alias);
        Beneficiario semAlias = gravados.Single(b => b.Id == IdBeneficiario.From(corpo.IdsBeneficiarios[2]));
        semAlias.Nome.Should().Be(nomeSemAlias);
        semAlias.Aliases.Should().BeEmpty();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_AliasJaDoMesmoBeneficiario_Deve_ManterUmAliasSo(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest anterior)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        Guid idExistente = (await _api.CadastrarBeneficiariosAsync(idUsuario, anterior))[0];
        string aliasEmOutraCaixa = anterior.Itens[0].Alias!.ToLowerInvariant();
        CadastrarLoteBeneficiariosRequest requisicao = new()
        {
            Itens = [new ItemBeneficiarioDto { IdBeneficiario = idExistente, Alias = aliasEmOutraCaixa }],
        };

        using HttpResponseMessage resposta = await CadastrarAsync(idUsuario, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        (await LerBeneficiariosAsync()).Single(b => b.Id == IdBeneficiario.From(idExistente)).Aliases.Should().ContainSingle()
            .Which.Alias.TextoOriginal.Should().Be(anterior.Itens[0].Alias);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_AliasDeOutroBeneficiario_Deve_RetornarConflitoSemGravar(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest anterior,
        ItemBeneficiarioDto novo)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        IReadOnlyList<Guid> ids = await _api.CadastrarBeneficiariosAsync(idUsuario, anterior);
        CadastrarLoteBeneficiariosRequest requisicao = new()
        {
            Itens = [novo, new ItemBeneficiarioDto { IdBeneficiario = ids[1], Alias = anterior.Itens[0].Alias }],
        };

        using HttpResponseMessage resposta = await CadastrarAsync(idUsuario, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await resposta.CamposComErroAsync()).Should().Equal("itens");
        List<Beneficiario> gravados = await LerBeneficiariosAsync();
        gravados.Should().HaveCount(anterior.Itens.Count);
        gravados.Single(b => b.Id == IdBeneficiario.From(ids[1])).Aliases.Should().ContainSingle();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_MesmoAliasParaDoisDestinosNoLote_Deve_RetornarConflitoSemGravar(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest requisicao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        CadastrarLoteBeneficiariosRequest repetida = requisicao with
        {
            Itens = [requisicao.Itens[0], requisicao.Itens[1] with { Alias = requisicao.Itens[0].Alias }],
        };

        using HttpResponseMessage resposta = await CadastrarAsync(idUsuario, repetida);

        resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await resposta.CamposComErroAsync()).Should().Equal("itens");
        (await LerBeneficiariosAsync()).Should().BeEmpty();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_MesmoAliasEmOutroUsuario_Deve_GravarNosDois(
        CadastrarUsuarioRequest cadastro,
        CadastrarUsuarioRequest outroCadastro,
        CadastrarLoteBeneficiariosRequest requisicao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        Guid idOutroUsuario = await _api.CadastrarUsuarioAsync(outroCadastro);
        await _api.CadastrarBeneficiariosAsync(idOutroUsuario, requisicao);

        using HttpResponseMessage resposta = await CadastrarAsync(idUsuario, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        (await LerBeneficiariosAsync())
            .GroupBy(b => b.IdUsuario)
            .Select(g => g.SelectMany(b => b.Aliases).Select(a => a.Alias.TextoOriginal))
            .Should().HaveCount(2).And.AllSatisfy(aliases => aliases.Should().BeEquivalentTo(requisicao.Itens.Select(i => i.Alias)));
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_BeneficiarioDoItemDeOutroUsuario_Deve_RetornarNaoEncontradoSemGravar(
        CadastrarUsuarioRequest cadastro,
        CadastrarUsuarioRequest outroCadastro,
        CadastrarLoteBeneficiariosRequest doOutro,
        CadastrarLoteBeneficiariosRequest requisicao,
        Alias alias)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        Guid idOutroUsuario = await _api.CadastrarUsuarioAsync(outroCadastro);
        Guid idDoOutro = (await _api.CadastrarBeneficiariosAsync(idOutroUsuario, doOutro))[0];
        CadastrarLoteBeneficiariosRequest comDoOutro = requisicao with
        {
            Itens = [.. requisicao.Itens, new ItemBeneficiarioDto { IdBeneficiario = idDoOutro, Alias = alias.TextoOriginal }],
        };

        using HttpResponseMessage resposta = await CadastrarAsync(idUsuario, comDoOutro);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        List<Beneficiario> gravados = await LerBeneficiariosAsync();
        gravados.Should().OnlyContain(b => b.IdUsuario == IdUsuario.From(idOutroUsuario));
        gravados.Single(b => b.Id == IdBeneficiario.From(idDoOutro)).Aliases.Should().ContainSingle();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_BeneficiarioDoItemNaoExiste_Deve_RetornarNaoEncontradoSemGravar(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest requisicao,
        Guid idBeneficiario,
        Alias alias)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        CadastrarLoteBeneficiariosRequest comInexistente = requisicao with
        {
            Itens = [.. requisicao.Itens, new ItemBeneficiarioDto { IdBeneficiario = idBeneficiario, Alias = alias.TextoOriginal }],
        };

        using HttpResponseMessage resposta = await CadastrarAsync(idUsuario, comInexistente);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await LerBeneficiariosAsync()).Should().BeEmpty();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_UsuarioNaoExiste_Deve_RetornarNaoEncontrado(
        Guid idUsuario,
        CadastrarLoteBeneficiariosRequest requisicao)
    {
        using HttpResponseMessage resposta = await CadastrarAsync(idUsuario, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await LerBeneficiariosAsync()).Should().BeEmpty();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_RequisicaoInvalida_Deve_RetornarErroEmCadaCampo(CadastrarLoteBeneficiariosRequest requisicao)
    {
        CadastrarLoteBeneficiariosRequest invalida = requisicao with
        {
            Itens =
            [
                requisicao.Itens[0] with { Nome = "" },
                requisicao.Itens[1] with { IdBeneficiario = Guid.Empty, Alias = " " },
            ],
        };

        using HttpResponseMessage resposta = await CadastrarAsync(Guid.Empty, invalida);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.CamposComErroAsync()).Should().BeEquivalentTo(
            "idUsuario",
            "itens[0].nome",
            "itens[1].idBeneficiario",
            "itens[1].nome",
            "itens[1].alias");
    }

    private Task<HttpResponseMessage> CadastrarAsync(Guid idUsuario, CadastrarLoteBeneficiariosRequest requisicao)
    {
        return _api.Client.PostAsJsonAsync(
            new Uri($"/api/usuarios/{idUsuario}/beneficiarios/lote", UriKind.Relative),
            requisicao);
    }

    private Task<List<Beneficiario>> LerBeneficiariosAsync() => _api.ObterTodosAsync(db => db.Beneficiarios);
}
