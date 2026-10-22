using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.AdicionarCartao;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ControleFinanceiro.WebApi.Tests.Usuarios.AdicionarCartao;

[Collection(ApiCollection.Nome)]
public sealed class AdicionarCartaoEndpointTests : IAsyncLifetime
{
    private static readonly Uri _rotaCadastro = new("/api/usuarios", UriKind.Relative);

    private readonly ApiComPostgresFixture _api;

    public AdicionarCartaoEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory, ApiAutoData]
    public async Task Post_Quando_RequisicaoValida_Deve_GravarOCartaoNoUsuario(
        [Frozen] Cartao esperado,
        CadastrarUsuarioRequest cadastro,
        AdicionarCartaoRequest requisicao)
    {
        Guid idUsuario = await CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await AdicionarAsync(idUsuario, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        AdicionarCartaoResponse? corpo = await resposta.Content.ReadFromJsonAsync<AdicionarCartaoResponse>();
        Usuario usuario = await LerUnicoUsuarioAsync();
        idUsuario.Should().Be(usuario.Id);
        Cartao gravado = usuario.Cartoes.Should().ContainSingle().Subject;
        corpo!.Id.Should().Be(gravado.Id);
        gravado.Banco.Should().BeSameAs(esperado.Banco);
        gravado.Should().BeEquivalentTo(esperado, options => options
            .ComparingByMembers<Cartao>()
            .Excluding(cartao => cartao.Id));
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_UsuarioJaTemOutroCartao_Deve_ManterOsDoisCartoes(
        CadastrarUsuarioRequest cadastro,
        AdicionarCartaoRequest primeiro,
        AdicionarCartaoRequest segundo)
    {
        Guid idUsuario = await CadastrarUsuarioAsync(cadastro);
        Guid idPrimeiro = await AdicionarComSucessoAsync(idUsuario, primeiro);

        Guid idSegundo = await AdicionarComSucessoAsync(idUsuario, segundo);

        Usuario usuario = await LerUnicoUsuarioAsync();
        usuario.Cartoes.Select(cartao => (Guid)cartao.Id).Should().BeEquivalentTo([idPrimeiro, idSegundo]);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_SufixoRepetidoNoMesmoBanco_Deve_RetornarConflitoNoSufixo(
        CadastrarUsuarioRequest cadastro,
        AdicionarCartaoRequest requisicao)
    {
        Guid idUsuario = await CadastrarUsuarioAsync(cadastro);
        await AdicionarComSucessoAsync(idUsuario, requisicao);

        using HttpResponseMessage resposta = await AdicionarAsync(idUsuario, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CamposComErroAsync(resposta)).Should().Equal("sufixo");
        (await ContarCartoesAsync()).Should().Be(1);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_UsuarioNaoExiste_Deve_RetornarNaoEncontrado(
        Guid idUsuario,
        AdicionarCartaoRequest requisicao)
    {
        using HttpResponseMessage resposta = await AdicionarAsync(idUsuario, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ContarCartoesAsync()).Should().Be(0);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_RequisicaoInvalida_Deve_RetornarErroEmCadaCampo(AdicionarCartaoRequest requisicao)
    {
        AdicionarCartaoRequest invalida = requisicao with { Banco = "999", Sufixo = "12a4", DiaVencimento = 0 };

        using HttpResponseMessage resposta = await AdicionarAsync(Guid.Empty, invalida);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CamposComErroAsync(resposta)).Should().BeEquivalentTo("idUsuario", "banco", "sufixo", "diaVencimento");
        (await ContarCartoesAsync()).Should().Be(0);
    }

    private async Task<Guid> CadastrarUsuarioAsync(CadastrarUsuarioRequest cadastro)
    {
        using HttpResponseMessage resposta = await _api.Client.PostAsJsonAsync(_rotaCadastro, cadastro);
        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        CadastrarUsuarioResponse? corpo = await resposta.Content.ReadFromJsonAsync<CadastrarUsuarioResponse>();
        return corpo!.Id;
    }

    private Task<HttpResponseMessage> AdicionarAsync(Guid idUsuario, AdicionarCartaoRequest requisicao)
    {
        return _api.Client.PostAsJsonAsync(
            new Uri($"/api/usuarios/{idUsuario}/cartoes", UriKind.Relative),
            requisicao);
    }

    private async Task<Guid> AdicionarComSucessoAsync(Guid idUsuario, AdicionarCartaoRequest requisicao)
    {
        using HttpResponseMessage resposta = await AdicionarAsync(idUsuario, requisicao);
        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        AdicionarCartaoResponse? corpo = await resposta.Content.ReadFromJsonAsync<AdicionarCartaoResponse>();
        return corpo!.Id;
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

    private Task<int> ContarCartoesAsync() =>
        _api.ConsultarAsync(db => db.Set<Cartao>().CountAsync());

    private Task<Usuario> LerUnicoUsuarioAsync() =>
        _api.ConsultarAsync(db => db.Usuarios.AsNoTracking().SingleAsync());
}
