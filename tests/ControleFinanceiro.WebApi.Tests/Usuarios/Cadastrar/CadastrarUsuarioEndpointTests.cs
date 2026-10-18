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
        corpo!.Id.Should().Be(gravado.Id.Value);
        gravado.Should().BeEquivalentTo(esperado, options => options
            .ComparingByMembers<Usuario>()
            .ComparingByMembers<DescontoSalarial>()
            .Excluding(membro => membro.Name == nameof(Usuario.Id)));
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_SemDescontos_Deve_GravarUsuarioSemDescontos(CadastrarUsuarioRequest requisicao)
    {
        using HttpResponseMessage resposta = await _api.Client.PostAsJsonAsync(
            _rota,
            requisicao with { DescontosSalariais = [] });

        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        Usuario gravado = await LerUnicoUsuarioAsync();
        gravado.DescontosSalariais.Should().BeEmpty();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_RendaBrutaZero_Deve_GravarUsuario(CadastrarUsuarioRequest requisicao)
    {
        using HttpResponseMessage resposta = await _api.Client.PostAsJsonAsync(
            _rota,
            requisicao with { RendaBruta = 0m });

        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        Usuario gravado = await LerUnicoUsuarioAsync();
        gravado.RendaBruta.Value.Should().Be(0m);
    }

    [Theory]
    [InlineApiAutoData(1)]
    [InlineApiAutoData(28)]
    public async Task Post_Quando_DiaVencimentoNoLimite_Deve_GravarUsuario(int diaVencimento, CadastrarUsuarioRequest requisicao)
    {
        using HttpResponseMessage resposta = await _api.Client.PostAsJsonAsync(
            _rota,
            requisicao with { Cartao = requisicao.Cartao with { DiaVencimento = diaVencimento } });

        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        Usuario gravado = await LerUnicoUsuarioAsync();
        gravado.Cartao.DiaVencimento.Value.Should().Be(diaVencimento);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_TextosComEspacosNasPontas_Deve_GravarTextosAparados(CadastrarUsuarioRequest requisicao)
    {
        DescontoSalarialDto desconto = requisicao.DescontosSalariais.First();
        CadastrarUsuarioRequest comEspacos = requisicao with
        {
            Nome = $"  {requisicao.Nome}  ",
            DescontosSalariais = [desconto with { Descricao = $"  {desconto.Descricao}  " }],
            Cartao = requisicao.Cartao with { Sufixo = $" {requisicao.Cartao.Sufixo} " },
        };

        using HttpResponseMessage resposta = await _api.Client.PostAsJsonAsync(_rota, comEspacos);

        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        Usuario gravado = await LerUnicoUsuarioAsync();
        gravado.Nome.Value.Should().Be(requisicao.Nome);
        gravado.DescontosSalariais.Should().ContainSingle().Which.Descricao.Value.Should().Be(desconto.Descricao);
        gravado.Cartao.Sufixo.Value.Should().Be(requisicao.Cartao.Sufixo);
    }

    [Theory]
    [InlineApiAutoData(null)]
    [InlineApiAutoData("")]
    [InlineApiAutoData("   ")]
    public async Task Post_Quando_NomeVazio_Deve_RetornarErroNoNome(string? nome, CadastrarUsuarioRequest requisicao)
    {
        await DeveRejeitarAsync(requisicao with { Nome = nome! }, "nome");
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_RendaBrutaNegativa_Deve_RetornarErroNaRenda(CadastrarUsuarioRequest requisicao)
    {
        await DeveRejeitarAsync(requisicao with { RendaBruta = -0.01m }, "rendaBruta");
    }

    [Theory]
    [InlineApiAutoData(null)]
    [InlineApiAutoData("")]
    [InlineApiAutoData("   ")]
    public async Task Post_Quando_DescricaoDoDescontoVazia_Deve_RetornarErroNaDescricao(string? descricao, CadastrarUsuarioRequest requisicao)
    {
        DescontoSalarialDto desconto = requisicao.DescontosSalariais.First() with { Descricao = descricao };

        await DeveRejeitarAsync(requisicao with { DescontosSalariais = [desconto] }, "descontosSalariais[0].descricao");
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_ValorDoDescontoNegativo_Deve_RetornarErroNoValor(CadastrarUsuarioRequest requisicao)
    {
        DescontoSalarialDto desconto = requisicao.DescontosSalariais.First() with { Valor = -0.01m };

        await DeveRejeitarAsync(requisicao with { DescontosSalariais = [desconto] }, "descontosSalariais[0].valor");
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_SemCartao_Deve_RetornarErroNoCartao(CadastrarUsuarioRequest requisicao)
    {
        await DeveRejeitarAsync(requisicao with { Cartao = null! }, "cartao");
    }

    [Theory]
    [InlineApiAutoData(null)]
    [InlineApiAutoData("")]
    [InlineApiAutoData("123")]
    [InlineApiAutoData("12345")]
    [InlineApiAutoData("12a4")]
    public async Task Post_Quando_SufixoDoCartaoSemQuatroDigitos_Deve_RetornarErroNoSufixo(string? sufixo, CadastrarUsuarioRequest requisicao)
    {
        await DeveRejeitarAsync(requisicao with { Cartao = requisicao.Cartao with { Sufixo = sufixo } }, "cartao.sufixo");
    }

    [Theory]
    [InlineApiAutoData(0)]
    [InlineApiAutoData(29)]
    public async Task Post_Quando_DiaVencimentoForaDe1A28_Deve_RetornarErroNoDia(int diaVencimento, CadastrarUsuarioRequest requisicao)
    {
        await DeveRejeitarAsync(
            requisicao with { Cartao = requisicao.Cartao with { DiaVencimento = diaVencimento } },
            "cartao.diaVencimento");
    }

    private async Task DeveRejeitarAsync(CadastrarUsuarioRequest requisicao, string campoComErro)
    {
        using HttpResponseMessage resposta = await _api.Client.PostAsJsonAsync(_rota, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CamposComErroAsync(resposta)).Should().Equal(campoComErro);
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

    private Task<Usuario> LerUnicoUsuarioAsync() =>
        _api.ConsultarAsync(db => db.Usuarios
            .AsNoTracking()
            .Include(usuario => usuario.DescontosSalariais)
            .SingleAsync());
}
