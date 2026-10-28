using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.WebApi.Endpoints.Beneficiarios.CadastrarLote;
using ControleFinanceiro.WebApi.Endpoints.Beneficiarios.Renomear;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;
using System.Net.Http.Json;

namespace ControleFinanceiro.WebApi.Tests.Beneficiarios.Renomear;

[Collection(ApiCollection.Nome)]
public sealed class RenomearBeneficiarioEndpointTests : IAsyncLifetime
{
    private readonly ApiComPostgresFixture _api;

    public RenomearBeneficiarioEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory, ApiAutoData]
    public async Task Put_Quando_RequisicaoValida_Deve_GravarONovoNomeSemMudarOsAliases(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        RenomearBeneficiarioRequest requisicao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        Guid idBeneficiario = (await _api.CadastrarBeneficiariosAsync(idUsuario, beneficiarios))[0];
        Beneficiario original = await LerBeneficiarioAsync(idBeneficiario);

        using HttpResponseMessage resposta = await RenomearAsync(idUsuario, idBeneficiario, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        Beneficiario gravado = await LerBeneficiarioAsync(idBeneficiario);
        gravado.Nome.Should().Be((TextoObrigatorio)requisicao.Nome);
        gravado.Aliases.Should().Equal(original.Aliases);
    }

    [Theory, ApiAutoData]
    public async Task Put_Quando_BeneficiarioDeOutroUsuario_Deve_RetornarNaoEncontrado(
        CadastrarUsuarioRequest cadastro,
        CadastrarUsuarioRequest outroCadastro,
        CadastrarLoteBeneficiariosRequest beneficiariosDoOutro,
        RenomearBeneficiarioRequest requisicao)
    {
        Guid idOutroUsuario = await _api.CadastrarUsuarioAsync(outroCadastro);
        Guid idBeneficiarioDoOutro = (await _api.CadastrarBeneficiariosAsync(idOutroUsuario, beneficiariosDoOutro))[0];
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await RenomearAsync(idUsuario, idBeneficiarioDoOutro, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await LerBeneficiarioAsync(idBeneficiarioDoOutro)).Nome.Should().Be((TextoObrigatorio)beneficiariosDoOutro.Itens[0].Nome);
    }

    [Theory, ApiAutoData]
    public async Task Put_Quando_RequisicaoInvalida_Deve_RetornarErroEmCadaCampo(RenomearBeneficiarioRequest requisicao)
    {
        using HttpResponseMessage resposta = await RenomearAsync(Guid.Empty, Guid.Empty, requisicao with { Nome = " " });

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.CamposComErroAsync()).Should().BeEquivalentTo("idUsuario", "idBeneficiario", "nome");
    }

    private Task<HttpResponseMessage> RenomearAsync(Guid idUsuario, Guid idBeneficiario, RenomearBeneficiarioRequest requisicao)
    {
        return _api.Client.PutAsJsonAsync(
            new Uri($"/api/usuarios/{idUsuario}/beneficiarios/{idBeneficiario}/nome", UriKind.Relative),
            requisicao);
    }

    private async Task<Beneficiario> LerBeneficiarioAsync(Guid idBeneficiario)
    {
        List<Beneficiario> beneficiarios = await _api.ObterTodosAsync(db => db.Beneficiarios);
        return beneficiarios.Should().ContainSingle(b => b.Id == IdBeneficiario.From(idBeneficiario)).Subject;
    }
}
