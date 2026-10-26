using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.WebApi.Endpoints.Categorias.CadastrarLote;
using ControleFinanceiro.WebApi.Endpoints.Categorias.Listar;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;
using System.Net.Http.Json;

namespace ControleFinanceiro.WebApi.Tests.Categorias.Listar;

[Collection(ApiCollection.Nome)]
public sealed class ListarCategoriasEndpointTests : IAsyncLifetime
{
    private readonly ApiComPostgresFixture _api;

    public ListarCategoriasEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory, ApiAutoData]
    public async Task Get_Quando_UsuarioComCategorias_Deve_ListarTodasEmOrdemAlfabeticaComOsAliases(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteCategoriasRequest categorias)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        IReadOnlyList<Guid> ids = await _api.CadastrarCategoriasAsync(idUsuario, categorias);
        Guid idOutros = (await _api.ObterTodosAsync(db => db.Categorias)).Single(c => c.EhOutros).Id.Value;

        using HttpResponseMessage resposta = await ListarAsync(idUsuario);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        ListarCategoriasResponse? corpo = await resposta.Content.ReadFromJsonAsync<ListarCategoriasResponse>();
        corpo!.Categorias.Select(c => c.Nome).Should().BeInAscendingOrder(StringComparer.CurrentCultureIgnoreCase);
        corpo.Categorias.Should().BeEquivalentTo(
        [
            new ListarCategoriasResponse.Categoria(idOutros, CategoriaDespesa.NomeOutros, Aliases: []),
            .. categorias.Itens.Select((item, i) => new ListarCategoriasResponse.Categoria(ids[i], item.Nome!, [item.Alias!])),
        ]);
    }

    [Theory, ApiAutoData]
    public async Task Get_Quando_UsuarioNaoExiste_Deve_RetornarNaoEncontrado(Guid idUsuario)
    {
        using HttpResponseMessage resposta = await ListarAsync(idUsuario);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Quando_IdUsuarioVazio_Deve_RetornarErroNoIdUsuario()
    {
        using HttpResponseMessage resposta = await ListarAsync(Guid.Empty);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.CamposComErroAsync()).Should().Equal("idUsuario");
    }

    private Task<HttpResponseMessage> ListarAsync(Guid idUsuario) =>
        _api.Client.GetAsync(new Uri($"/api/usuarios/{idUsuario}/categorias", UriKind.Relative));
}
