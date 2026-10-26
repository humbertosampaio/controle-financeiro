using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.WebApi.Endpoints.Categorias.CadastrarLote;
using ControleFinanceiro.WebApi.Endpoints.Categorias.Renomear;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;
using System.Net.Http.Json;

namespace ControleFinanceiro.WebApi.Tests.Categorias.Renomear;

[Collection(ApiCollection.Nome)]
public sealed class RenomearCategoriaEndpointTests : IAsyncLifetime
{
    private readonly ApiComPostgresFixture _api;

    public RenomearCategoriaEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory, ApiAutoData]
    public async Task Put_Quando_RequisicaoValida_Deve_GravarONovoNomeSemMudarOsAliases(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteCategoriasRequest categorias,
        RenomearCategoriaRequest requisicao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        Guid idCategoria = (await _api.CadastrarCategoriasAsync(idUsuario, categorias))[0];
        CategoriaDespesa original = await LerCategoriaAsync(idCategoria);

        using HttpResponseMessage resposta = await RenomearAsync(idUsuario, idCategoria, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        CategoriaDespesa gravada = await LerCategoriaAsync(idCategoria);
        gravada.Nome.Should().Be((TextoObrigatorio)requisicao.Nome);
        gravada.Aliases.Should().Equal(original.Aliases);
    }

    [Theory, ApiAutoData]
    public async Task Put_Quando_CategoriaOutros_Deve_RetornarConflitoNoNome(
        CadastrarUsuarioRequest cadastro,
        RenomearCategoriaRequest requisicao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        Guid idOutros = (await _api.ObterTodosAsync(db => db.Categorias)).Single().Id.Value;

        using HttpResponseMessage resposta = await RenomearAsync(idUsuario, idOutros, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await resposta.CamposComErroAsync()).Should().Equal("nome");
        (await LerCategoriaAsync(idOutros)).Nome.Should().Be(CategoriaDespesa.NomeOutros);
    }

    [Theory, ApiAutoData]
    public async Task Put_Quando_NomeDeOutraCategoriaComOutraCaixaEAcento_Deve_RetornarConflitoNoNome(
        CadastrarUsuarioRequest cadastro,
        RenomearCategoriaRequest requisicao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        IReadOnlyList<Guid> ids = await _api.CadastrarCategoriasAsync(idUsuario, new CadastrarLoteCategoriasRequest
        {
            Itens = [new ItemCategoriaDto { Nome = "Saúde" }, new ItemCategoriaDto { Nome = "Lazer" }],
        });

        using HttpResponseMessage resposta = await RenomearAsync(idUsuario, ids[1], requisicao with { Nome = "SAUDE" });

        resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await resposta.CamposComErroAsync()).Should().Equal("nome");
        (await LerCategoriaAsync(ids[1])).Nome.Should().Be((TextoObrigatorio)"Lazer");
    }

    [Theory, ApiAutoData]
    public async Task Put_Quando_CategoriaDeOutroUsuario_Deve_RetornarNaoEncontrado(
        CadastrarUsuarioRequest cadastro,
        CadastrarUsuarioRequest outroCadastro,
        CadastrarLoteCategoriasRequest categoriasDoOutro,
        RenomearCategoriaRequest requisicao)
    {
        Guid idOutroUsuario = await _api.CadastrarUsuarioAsync(outroCadastro);
        Guid idCategoriaDoOutro = (await _api.CadastrarCategoriasAsync(idOutroUsuario, categoriasDoOutro))[0];
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await RenomearAsync(idUsuario, idCategoriaDoOutro, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await LerCategoriaAsync(idCategoriaDoOutro)).Nome.Should().Be((TextoObrigatorio)categoriasDoOutro.Itens[0].Nome);
    }

    [Theory, ApiAutoData]
    public async Task Put_Quando_RequisicaoInvalida_Deve_RetornarErroEmCadaCampo(RenomearCategoriaRequest requisicao)
    {
        using HttpResponseMessage resposta = await RenomearAsync(Guid.Empty, Guid.Empty, requisicao with { Nome = " " });

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.CamposComErroAsync()).Should().BeEquivalentTo("idUsuario", "idCategoria", "nome");
    }

    private Task<HttpResponseMessage> RenomearAsync(Guid idUsuario, Guid idCategoria, RenomearCategoriaRequest requisicao)
    {
        return _api.Client.PutAsJsonAsync(
            new Uri($"/api/usuarios/{idUsuario}/categorias/{idCategoria}/nome", UriKind.Relative),
            requisicao);
    }

    private async Task<CategoriaDespesa> LerCategoriaAsync(Guid idCategoria)
    {
        List<CategoriaDespesa> categorias = await _api.ObterTodosAsync(db => db.Categorias);
        return categorias.Should().ContainSingle(c => c.Id == IdCategoriaDespesa.From(idCategoria)).Subject;
    }
}
