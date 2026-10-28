using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.WebApi.Endpoints.Beneficiarios.CadastrarLote;
using ControleFinanceiro.WebApi.Endpoints.Beneficiarios.RemoverAlias;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;
using System.Net.Http.Json;

namespace ControleFinanceiro.WebApi.Tests.Beneficiarios.RemoverAlias;

[Collection(ApiCollection.Nome)]
public sealed class RemoverAliasBeneficiarioEndpointTests : IAsyncLifetime
{
    private readonly ApiComPostgresFixture _api;

    public RemoverAliasBeneficiarioEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory, ApiAutoData]
    public async Task Post_Quando_BeneficiarioPossuiOAlias_Deve_RemoverSoDele(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        RemoverAliasBeneficiarioRequest requisicao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        IReadOnlyList<Guid> ids = await _api.CadastrarBeneficiariosAsync(idUsuario, beneficiarios);

        using HttpResponseMessage resposta = await RemoverAsync(idUsuario, ids[0], requisicao with { Alias = beneficiarios.Itens[0].Alias });

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        List<Beneficiario> gravados = await LerBeneficiariosAsync();
        gravados.Single(b => b.Id == IdBeneficiario.From(ids[0])).Aliases.Should().BeEmpty();
        gravados.Single(b => b.Id == IdBeneficiario.From(ids[1])).Aliases.Should().ContainSingle();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_AliasRemovido_Deve_PoderSerAtribuidoAOutroBeneficiario(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        RemoverAliasBeneficiarioRequest requisicao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        IReadOnlyList<Guid> ids = await _api.CadastrarBeneficiariosAsync(idUsuario, beneficiarios);
        string alias = beneficiarios.Itens[0].Alias!;
        using HttpResponseMessage remocao = await RemoverAsync(idUsuario, ids[0], requisicao with { Alias = alias });
        remocao.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await _api.CadastrarBeneficiariosAsync(idUsuario, new()
        {
            Itens = [new ItemBeneficiarioDto { IdBeneficiario = ids[1], Alias = alias }],
        });

        List<Beneficiario> gravados = await LerBeneficiariosAsync();
        gravados.Single(b => b.Id == IdBeneficiario.From(ids[0])).Aliases.Should().BeEmpty();
        gravados.Single(b => b.Id == IdBeneficiario.From(ids[1])).Aliases.Select(a => a.Alias.TextoOriginal).Should().Contain(alias);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_AliasRemovido_Deve_PoderVoltarAoMesmoBeneficiario(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        RemoverAliasBeneficiarioRequest requisicao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        IReadOnlyList<Guid> ids = await _api.CadastrarBeneficiariosAsync(idUsuario, beneficiarios);
        string alias = beneficiarios.Itens[0].Alias!;
        using HttpResponseMessage remocao = await RemoverAsync(idUsuario, ids[0], requisicao with { Alias = alias });
        remocao.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await _api.CadastrarBeneficiariosAsync(idUsuario, new()
        {
            Itens = [new ItemBeneficiarioDto { IdBeneficiario = ids[0], Alias = alias }],
        });

        (await LerBeneficiariosAsync()).Single(b => b.Id == IdBeneficiario.From(ids[0])).Aliases.Should().ContainSingle()
            .Which.Alias.TextoOriginal.Should().Be(alias);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_BeneficiarioDeOutroUsuario_Deve_RetornarNaoEncontrado(
        CadastrarUsuarioRequest cadastro,
        CadastrarUsuarioRequest outroCadastro,
        CadastrarLoteBeneficiariosRequest beneficiariosDoOutro,
        RemoverAliasBeneficiarioRequest requisicao)
    {
        Guid idOutroUsuario = await _api.CadastrarUsuarioAsync(outroCadastro);
        Guid idBeneficiarioDoOutro = (await _api.CadastrarBeneficiariosAsync(idOutroUsuario, beneficiariosDoOutro))[0];
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await RemoverAsync(
            idUsuario,
            idBeneficiarioDoOutro,
            requisicao with { Alias = beneficiariosDoOutro.Itens[0].Alias });

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await LerBeneficiariosAsync()).Single(b => b.Id == IdBeneficiario.From(idBeneficiarioDoOutro)).Aliases.Should().ContainSingle();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_RequisicaoInvalida_Deve_RetornarErroEmCadaCampo(RemoverAliasBeneficiarioRequest requisicao)
    {
        using HttpResponseMessage resposta = await RemoverAsync(Guid.Empty, Guid.Empty, requisicao with { Alias = "" });

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.CamposComErroAsync()).Should().BeEquivalentTo("idUsuario", "idBeneficiario", "alias");
    }

    private Task<HttpResponseMessage> RemoverAsync(Guid idUsuario, Guid idBeneficiario, RemoverAliasBeneficiarioRequest requisicao)
    {
        return _api.Client.PostAsJsonAsync(
            new Uri($"/api/usuarios/{idUsuario}/beneficiarios/{idBeneficiario}/aliases/remocao", UriKind.Relative),
            requisicao);
    }

    private Task<List<Beneficiario>> LerBeneficiariosAsync() => _api.ObterTodosAsync(db => db.Beneficiarios);
}
