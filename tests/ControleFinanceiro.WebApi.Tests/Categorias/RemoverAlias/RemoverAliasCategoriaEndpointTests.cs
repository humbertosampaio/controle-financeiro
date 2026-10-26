using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.WebApi.Endpoints.Categorias.CadastrarLote;
using ControleFinanceiro.WebApi.Endpoints.Categorias.RemoverAlias;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;
using System.Net.Http.Json;

namespace ControleFinanceiro.WebApi.Tests.Categorias.RemoverAlias;

[Collection(ApiCollection.Nome)]
public sealed class RemoverAliasCategoriaEndpointTests : IAsyncLifetime
{
    private readonly ApiComPostgresFixture _api;

    public RemoverAliasCategoriaEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory, ApiAutoData]
    public async Task Post_Quando_CategoriaPossuiOAlias_Deve_RemoverSoDela(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteCategoriasRequest categorias,
        RemoverAliasCategoriaRequest requisicao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        IReadOnlyList<Guid> ids = await _api.CadastrarCategoriasAsync(idUsuario, categorias);

        using HttpResponseMessage resposta = await RemoverAsync(idUsuario, ids[0], requisicao with { Alias = categorias.Itens[0].Alias });

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        List<CategoriaDespesa> gravadas = await _api.ObterTodosAsync(db => db.Categorias);
        gravadas.Single(c => c.Id == IdCategoriaDespesa.From(ids[0])).Aliases.Should().BeEmpty();
        gravadas.Single(c => c.Id == IdCategoriaDespesa.From(ids[1])).Aliases.Should().ContainSingle();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_CategoriaDeOutroUsuario_Deve_RetornarNaoEncontrado(
        CadastrarUsuarioRequest cadastro,
        CadastrarUsuarioRequest outroCadastro,
        CadastrarLoteCategoriasRequest categoriasDoOutro,
        RemoverAliasCategoriaRequest requisicao)
    {
        Guid idOutroUsuario = await _api.CadastrarUsuarioAsync(outroCadastro);
        Guid idCategoriaDoOutro = (await _api.CadastrarCategoriasAsync(idOutroUsuario, categoriasDoOutro))[0];
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await RemoverAsync(
            idUsuario,
            idCategoriaDoOutro,
            requisicao with { Alias = categoriasDoOutro.Itens[0].Alias });

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _api.ObterTodosAsync(db => db.Categorias))
            .Single(c => c.Id == IdCategoriaDespesa.From(idCategoriaDoOutro)).Aliases.Should().ContainSingle();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_RequisicaoInvalida_Deve_RetornarErroEmCadaCampo(RemoverAliasCategoriaRequest requisicao)
    {
        using HttpResponseMessage resposta = await RemoverAsync(Guid.Empty, Guid.Empty, requisicao with { Alias = "" });

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.CamposComErroAsync()).Should().BeEquivalentTo("idUsuario", "idCategoria", "alias");
    }

    private Task<HttpResponseMessage> RemoverAsync(Guid idUsuario, Guid idCategoria, RemoverAliasCategoriaRequest requisicao)
    {
        return _api.Client.PostAsJsonAsync(
            new Uri($"/api/usuarios/{idUsuario}/categorias/{idCategoria}/aliases/remocao", UriKind.Relative),
            requisicao);
    }
}
