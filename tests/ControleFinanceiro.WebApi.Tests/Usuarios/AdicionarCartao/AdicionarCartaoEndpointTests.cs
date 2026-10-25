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

namespace ControleFinanceiro.WebApi.Tests.Usuarios.AdicionarCartao;

[Collection(ApiCollection.Nome)]
public sealed class AdicionarCartaoEndpointTests : IAsyncLifetime
{
    private readonly ApiComPostgresFixture _api;

    public AdicionarCartaoEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory, ApiAutoData]
    public async Task Post_Quando_RequisicaoValida_Deve_GravarOCartaoDoUsuario(
        [Frozen] Cartao esperado,
        CadastrarUsuarioRequest cadastro,
        AdicionarCartaoRequest requisicao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await AdicionarAsync(idUsuario, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        AdicionarCartaoResponse? corpo = await resposta.Content.ReadFromJsonAsync<AdicionarCartaoResponse>();
        Cartao gravado = (await _api.LerTodosAsync(db => db.Cartoes)).Should().ContainSingle().Subject;
        corpo!.Id.Should().Be(gravado.Id);
        gravado.IdUsuario.Should().Be(IdUsuario.From(idUsuario));
        gravado.Banco.Should().BeSameAs(esperado.Banco);
        gravado.Should().BeEquivalentTo(esperado, options => options
            .ComparingByMembers<Cartao>()
            .Excluding(cartao => cartao.Id)
            .Excluding(cartao => cartao.IdUsuario));
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_UsuarioJaTemOutroCartao_Deve_ManterOsDoisCartoes(
        CadastrarUsuarioRequest cadastro,
        AdicionarCartaoRequest primeiro,
        AdicionarCartaoRequest segundo)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        Guid idPrimeiro = await AdicionarComSucessoAsync(idUsuario, primeiro);

        Guid idSegundo = await AdicionarComSucessoAsync(idUsuario, segundo);

        (await _api.LerTodosAsync(db => db.Cartoes)).Select(cartao => (Guid)cartao.Id).Should().BeEquivalentTo([idPrimeiro, idSegundo]);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_OutroUsuarioTemOMesmoCartao_Deve_Gravar(
        CadastrarUsuarioRequest cadastro,
        CadastrarUsuarioRequest outroCadastro,
        AdicionarCartaoRequest requisicao)
    {
        Guid idOutroUsuario = await _api.CadastrarUsuarioAsync(outroCadastro);
        await AdicionarComSucessoAsync(idOutroUsuario, requisicao);
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        Guid idCartao = await AdicionarComSucessoAsync(idUsuario, requisicao);

        List<Cartao> cartoes = await _api.LerTodosAsync(db => db.Cartoes);
        cartoes.Should().HaveCount(2);
        cartoes.Should().ContainSingle(cartao => cartao.Id == IdCartao.From(idCartao))
            .Which.IdUsuario.Should().Be(IdUsuario.From(idUsuario));
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_SufixoRepetidoNoMesmoBanco_Deve_RetornarConflitoNoSufixo(
        CadastrarUsuarioRequest cadastro,
        AdicionarCartaoRequest requisicao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await AdicionarComSucessoAsync(idUsuario, requisicao);

        using HttpResponseMessage resposta = await AdicionarAsync(idUsuario, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await resposta.CamposComErroAsync()).Should().Equal("sufixo");
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
        (await resposta.CamposComErroAsync()).Should().BeEquivalentTo("idUsuario", "banco", "sufixo", "diaVencimento");
        (await ContarCartoesAsync()).Should().Be(0);
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

    private Task<int> ContarCartoesAsync() =>
        _api.ConsultarAsync(db => db.Cartoes.CountAsync());
}
