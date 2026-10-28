using ControleFinanceiro.WebApi.Endpoints.Beneficiarios.CadastrarLote;
using ControleFinanceiro.WebApi.Endpoints.Beneficiarios.Listar;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;
using System.Net.Http.Json;

namespace ControleFinanceiro.WebApi.Tests.Beneficiarios.Listar;

[Collection(ApiCollection.Nome)]
public sealed class ListarBeneficiariosEndpointTests : IAsyncLifetime
{
    private readonly ApiComPostgresFixture _api;

    public ListarBeneficiariosEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory, ApiAutoData]
    public async Task Get_Quando_UsuarioComBeneficiarios_Deve_ListarTodosEmOrdemAlfabeticaComOsAliases(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        IReadOnlyList<Guid> ids = await _api.CadastrarBeneficiariosAsync(idUsuario, beneficiarios);

        using HttpResponseMessage resposta = await ListarAsync(idUsuario);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        ListarBeneficiariosResponse? corpo = await resposta.Content.ReadFromJsonAsync<ListarBeneficiariosResponse>();
        corpo!.Beneficiarios.Select(b => b.Nome).Should().BeInAscendingOrder(StringComparer.CurrentCultureIgnoreCase);
        corpo.Beneficiarios.Should().BeEquivalentTo(
            beneficiarios.Itens.Select((item, i) => new ListarBeneficiariosResponse.Beneficiario(ids[i], item.Nome!, [item.Alias!])));
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
        _api.Client.GetAsync(new Uri($"/api/usuarios/{idUsuario}/beneficiarios", UriKind.Relative));
}
