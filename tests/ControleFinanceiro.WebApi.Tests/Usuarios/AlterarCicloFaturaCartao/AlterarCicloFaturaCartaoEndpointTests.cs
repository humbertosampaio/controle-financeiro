using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.AdicionarCartao;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.AlterarCicloFaturaCartao;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;
using System.Net.Http.Json;

namespace ControleFinanceiro.WebApi.Tests.Usuarios.AlterarCicloFaturaCartao;

[Collection(ApiCollection.Nome)]
public sealed class AlterarCicloFaturaCartaoEndpointTests : IAsyncLifetime
{
    private readonly ApiComPostgresFixture _api;

    public AlterarCicloFaturaCartaoEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory, ApiAutoData]
    public async Task Put_Quando_RequisicaoValida_Deve_GravarOsNovosDias(
        CadastrarUsuarioRequest cadastro,
        AdicionarCartaoRequest cartao,
        AlterarCicloFaturaCartaoRequest requisicao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        Guid idCartao = await _api.AdicionarCartaoAsync(idUsuario, cartao);
        Cartao original = await LerCartaoAsync(idCartao);

        using HttpResponseMessage resposta = await AlterarAsync(idUsuario, idCartao, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        Cartao gravado = await LerCartaoAsync(idCartao);
        gravado.DiaVencimento.Should().Be((DiaVencimentoCartao)requisicao.DiaVencimento);
        gravado.DiaFechamento.Should().Be((DiaFechamentoCartao)requisicao.DiaFechamento);
        gravado.Should().BeEquivalentTo(original, options => options
            .ComparingByMembers<Cartao>()
            .Excluding(c => c.DiaVencimento)
            .Excluding(c => c.DiaFechamento));
    }

    [Theory, ApiAutoData]
    public async Task Put_Quando_CartaoDeOutroUsuario_Deve_RetornarNaoEncontrado(
        CadastrarUsuarioRequest cadastro,
        CadastrarUsuarioRequest outroCadastro,
        AdicionarCartaoRequest cartaoDoOutro,
        AlterarCicloFaturaCartaoRequest requisicao)
    {
        Guid idOutroUsuario = await _api.CadastrarUsuarioAsync(outroCadastro);
        Guid idCartaoDoOutro = await _api.AdicionarCartaoAsync(idOutroUsuario, cartaoDoOutro);
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await AlterarAsync(idUsuario, idCartaoDoOutro, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await LerCartaoAsync(idCartaoDoOutro)).DiaVencimento.Should().Be((DiaVencimentoCartao)cartaoDoOutro.DiaVencimento);
    }

    [Theory, ApiAutoData]
    public async Task Put_Quando_CartaoNaoExiste_Deve_RetornarNaoEncontrado(
        CadastrarUsuarioRequest cadastro,
        Guid idCartao,
        AlterarCicloFaturaCartaoRequest requisicao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await AlterarAsync(idUsuario, idCartao, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory, ApiAutoData]
    public async Task Put_Quando_RequisicaoInvalida_Deve_RetornarErroEmCadaCampo(AlterarCicloFaturaCartaoRequest requisicao)
    {
        AlterarCicloFaturaCartaoRequest invalida = requisicao with { DiaVencimento = 0, DiaFechamento = 29 };

        using HttpResponseMessage resposta = await AlterarAsync(Guid.Empty, Guid.Empty, invalida);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.CamposComErroAsync()).Should().BeEquivalentTo("idUsuario", "idCartao", "diaVencimento", "diaFechamento");
    }

    private Task<HttpResponseMessage> AlterarAsync(Guid idUsuario, Guid idCartao, AlterarCicloFaturaCartaoRequest requisicao)
    {
        return _api.Client.PutAsJsonAsync(
            new Uri($"/api/usuarios/{idUsuario}/cartoes/{idCartao}/ciclo-fatura", UriKind.Relative),
            requisicao);
    }

    private async Task<Cartao> LerCartaoAsync(Guid idCartao)
    {
        List<Cartao> cartoes = await _api.ObterTodosAsync(db => db.Cartoes);
        return cartoes.Should().ContainSingle(c => c.Id == IdCartao.From(idCartao)).Subject;
    }
}
