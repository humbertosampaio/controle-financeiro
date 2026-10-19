using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.WebApi.Tests.Usuarios.Cadastrar;

[Collection(ApiCollection.Nome)]
public sealed class CadastrarUsuarioEndpointTests : IAsyncLifetime
{
    private static readonly Uri _rota = new("/api/usuarios", UriKind.Relative);

    private readonly ApiComPostgresFixture _api;

    public CadastrarUsuarioEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory, ApiAutoData]
    public async Task Post_Quando_RequisicaoValida_Deve_GravarOUsuarioRecebido(
        [Frozen] Usuario esperado,
        CadastrarUsuarioRequest requisicao)
    {
        using HttpResponseMessage resposta = await _api.Client.PostAsJsonAsync(_rota, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        CadastrarUsuarioResponse? corpo = await resposta.Content.ReadFromJsonAsync<CadastrarUsuarioResponse>();
        Usuario gravado = await LerUnicoUsuarioAsync();
        corpo!.Id.Should().Be(gravado.Id);
        gravado.Should().BeEquivalentTo(esperado, options => options
            .ComparingByMembers<Usuario>()
            .ComparingByMembers<DescontoSalarial>()
            .Excluding(membro => membro.Name == nameof(Usuario.Id)));
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_RequisicaoInvalida_Deve_RetornarErroEmCadaCampo(CadastrarUsuarioRequest requisicao)
    {
        DescontoSalarialDto desconto = requisicao.DescontosSalariais.First() with { Descricao = "", Valor = -0.01m };
        CadastrarUsuarioRequest invalida = requisicao with
        {
            Nome = "",
            RendaBruta = -0.01m,
            DescontosSalariais = [desconto],
        };

        using HttpResponseMessage resposta = await _api.Client.PostAsJsonAsync(_rota, invalida);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CamposComErroAsync(resposta)).Should().BeEquivalentTo(
            "nome",
            "rendaBruta",
            "descontosSalariais[0].descricao",
            "descontosSalariais[0].valor");
        (await _api.ConsultarAsync(db => db.Usuarios.CountAsync())).Should().Be(0);
    }

    private static async Task<IReadOnlyList<string>> CamposComErroAsync(HttpResponseMessage resposta)
    {
        using JsonDocument problema = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return problema.RootElement
            .GetProperty("errors")
            .EnumerateArray()
            .Select(erro => erro.GetProperty("name").GetString()!)
            .Distinct()
            .ToList();
    }

    private Task<Usuario> LerUnicoUsuarioAsync()
    {
        return _api.ConsultarAsync(db => db.Usuarios.AsNoTracking().SingleAsync());
    }
}
