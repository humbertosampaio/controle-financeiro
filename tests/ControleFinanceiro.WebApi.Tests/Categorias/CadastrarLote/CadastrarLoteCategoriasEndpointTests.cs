using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.WebApi.Endpoints.Categorias.CadastrarLote;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;
using System.Net.Http.Json;

namespace ControleFinanceiro.WebApi.Tests.Categorias.CadastrarLote;

[Collection(ApiCollection.Nome)]
public sealed class CadastrarLoteCategoriasEndpointTests : IAsyncLifetime
{
    private readonly ApiComPostgresFixture _api;

    public CadastrarLoteCategoriasEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory, ApiAutoData]
    public async Task Post_Quando_CategoriasNovas_Deve_GravarCadaUmaComOAlias(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteCategoriasRequest requisicao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await CadastrarAsync(idUsuario, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        CadastrarLoteCategoriasResponse? corpo = await resposta.Content.ReadFromJsonAsync<CadastrarLoteCategoriasResponse>();
        List<CategoriaDespesa> novas = [.. (await LerCategoriasAsync()).Where(c => !c.EhOutros)];
        novas.Should().OnlyContain(c => c.IdUsuario == IdUsuario.From(idUsuario));
        novas.Select(c => ((Guid)c.Id, c.Nome.Value, c.Aliases.Single().Alias.TextoOriginal))
            .Should().BeEquivalentTo(requisicao.Itens.Select((item, i) => (corpo!.IdsCategorias[i], item.Nome, item.Alias)));
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_AliasNaCategoriaExistenteENovaSemAlias_Deve_GravarOsDoisItens(
        CadastrarUsuarioRequest cadastro,
        Alias alias,
        TextoObrigatorio nome)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        CategoriaDespesa outros = (await LerCategoriasAsync()).Single();
        CadastrarLoteCategoriasRequest requisicao = new()
        {
            Itens =
            [
                new ItemCategoriaDto { IdCategoria = outros.Id.Value, Alias = alias.TextoOriginal },
                new ItemCategoriaDto { Nome = nome },
            ],
        };

        using HttpResponseMessage resposta = await CadastrarAsync(idUsuario, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        CadastrarLoteCategoriasResponse? corpo = await resposta.Content.ReadFromJsonAsync<CadastrarLoteCategoriasResponse>();
        List<CategoriaDespesa> gravadas = await LerCategoriasAsync();
        gravadas.Single(c => c.Id == outros.Id).Aliases.Should().ContainSingle()
            .Which.Alias.TextoOriginal.Should().Be(alias.TextoOriginal);
        CategoriaDespesa nova = gravadas.Should().ContainSingle(c => c.Id != outros.Id).Subject;
        nova.Nome.Should().Be(nome);
        nova.Aliases.Should().BeEmpty();
        corpo!.IdsCategorias.Should().Equal(outros.Id.Value, nova.Id.Value);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_NomeDaOutrosComOutraCaixa_Deve_RetornarConflitoSemGravar(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteCategoriasRequest requisicao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        CadastrarLoteCategoriasRequest repetida = requisicao with
        {
            Itens = [.. requisicao.Itens.Take(1), requisicao.Itens[1] with { Nome = "OUTROS" }],
        };

        using HttpResponseMessage resposta = await CadastrarAsync(idUsuario, repetida);

        resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await resposta.CamposComErroAsync()).Should().Equal("itens");
        (await LerCategoriasAsync()).Should().ContainSingle().Which.EhOutros.Should().BeTrue();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_AliasDeOutraCategoria_Deve_RetornarConflitoSemGravar(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteCategoriasRequest anterior)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await _api.CadastrarCategoriasAsync(idUsuario, anterior);
        CategoriaDespesa outros = (await LerCategoriasAsync()).Single(c => c.EhOutros);
        CadastrarLoteCategoriasRequest requisicao = new()
        {
            Itens = [new ItemCategoriaDto { IdCategoria = outros.Id.Value, Alias = anterior.Itens[0].Alias }],
        };

        using HttpResponseMessage resposta = await CadastrarAsync(idUsuario, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await resposta.CamposComErroAsync()).Should().Equal("itens");
        (await LerCategoriasAsync()).Single(c => c.EhOutros).Aliases.Should().BeEmpty();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_CategoriaDoItemNaoExiste_Deve_RetornarNaoEncontradoSemGravar(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteCategoriasRequest requisicao,
        Guid idCategoria,
        Alias alias)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        CadastrarLoteCategoriasRequest comInexistente = requisicao with
        {
            Itens = [.. requisicao.Itens, new ItemCategoriaDto { IdCategoria = idCategoria, Alias = alias.TextoOriginal }],
        };

        using HttpResponseMessage resposta = await CadastrarAsync(idUsuario, comInexistente);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await LerCategoriasAsync()).Should().ContainSingle();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_UsuarioNaoExiste_Deve_RetornarNaoEncontrado(
        Guid idUsuario,
        CadastrarLoteCategoriasRequest requisicao)
    {
        using HttpResponseMessage resposta = await CadastrarAsync(idUsuario, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await LerCategoriasAsync()).Should().BeEmpty();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_RequisicaoInvalida_Deve_RetornarErroEmCadaCampo(CadastrarLoteCategoriasRequest requisicao)
    {
        CadastrarLoteCategoriasRequest invalida = requisicao with
        {
            Itens =
            [
                requisicao.Itens[0] with { Nome = "" },
                requisicao.Itens[1] with { IdCategoria = Guid.Empty, Alias = " " },
            ],
        };

        using HttpResponseMessage resposta = await CadastrarAsync(Guid.Empty, invalida);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.CamposComErroAsync()).Should().BeEquivalentTo(
            "idUsuario",
            "itens[0].nome",
            "itens[1].idCategoria",
            "itens[1].nome",
            "itens[1].alias");
    }

    private Task<HttpResponseMessage> CadastrarAsync(Guid idUsuario, CadastrarLoteCategoriasRequest requisicao)
    {
        return _api.Client.PostAsJsonAsync(
            new Uri($"/api/usuarios/{idUsuario}/categorias/lote", UriKind.Relative),
            requisicao);
    }

    private Task<List<CategoriaDespesa>> LerCategoriasAsync() => _api.ObterTodosAsync(db => db.Categorias);
}
