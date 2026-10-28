using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.WebApi.Endpoints.Beneficiarios.CadastrarLote;
using ControleFinanceiro.WebApi.Endpoints.Despesas.CadastrarRecorrente;
using ControleFinanceiro.WebApi.Endpoints.Despesas.ListarRecorrentes;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;
using System.Net.Http.Json;

namespace ControleFinanceiro.WebApi.Tests.Despesas.ListarRecorrentes;

[Collection(ApiCollection.Nome)]
public sealed class ListarDespesasRecorrentesEndpointTests : IAsyncLifetime
{
    private readonly ApiComPostgresFixture _api;

    public ListarDespesasRecorrentesEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory, ApiAutoData]
    public async Task Get_Quando_UsuarioComRecorrentes_Deve_ListarSoAsDelePorDiaDeOcorrencia(
        CadastrarUsuarioRequest cadastro,
        CadastrarUsuarioRequest outroCadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        CadastrarDespesaRecorrenteRequest requisicao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        CadastrarDespesaRecorrenteRequest semCartao = await ComItensDoUsuarioAsync(idUsuario, beneficiarios, requisicao);
        CadastrarDespesaRecorrenteRequest dia20 = semCartao with { Descricao = "Aluguel", DiaOcorrencia = 20 };
        CadastrarDespesaRecorrenteRequest dia5 = semCartao with { Descricao = "Internet", DiaOcorrencia = 5 };
        Guid idDia20 = await CadastrarAsync(idUsuario, dia20);
        Guid idDia5 = await CadastrarAsync(idUsuario, dia5);
        Guid idOutroUsuario = await _api.CadastrarUsuarioAsync(outroCadastro);
        await CadastrarAsync(idOutroUsuario, await ComItensDoUsuarioAsync(idOutroUsuario, beneficiarios, requisicao));

        using HttpResponseMessage resposta = await ListarAsync(idUsuario);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        ListarDespesasRecorrentesResponse? corpo = await resposta.Content.ReadFromJsonAsync<ListarDespesasRecorrentesResponse>();
        corpo!.DespesasRecorrentes.Should().Equal(
            Esperada(idDia5, dia5),
            Esperada(idDia20, dia20));
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

    /// <summary>A requisição no Pix, com um beneficiário cadastrado e a categoria Outros do usuário.</summary>
    private async Task<CadastrarDespesaRecorrenteRequest> ComItensDoUsuarioAsync(
        Guid idUsuario,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        CadastrarDespesaRecorrenteRequest requisicao)
    {
        IReadOnlyList<Guid> idsBeneficiarios = await _api.CadastrarBeneficiariosAsync(idUsuario, beneficiarios);
        Guid idOutros = (await _api.ObterTodosAsync(db => db.Categorias))
            .Single(c => c.IdUsuario == IdUsuario.From(idUsuario))
            .Id.Value;

        return requisicao with
        {
            IdBeneficiario = idsBeneficiarios[0],
            IdCategoria = idOutros,
            FormaPagamento = FormaPagamento.Pix.Value,
            IdCartao = null,
        };
    }

    private static ListarDespesasRecorrentesResponse.DespesaRecorrente Esperada(Guid id, CadastrarDespesaRecorrenteRequest requisicao)
    {
        return new(
            id,
            requisicao.IdBeneficiario,
            requisicao.Descricao!,
            requisicao.IdCategoria,
            requisicao.FormaPagamento,
            requisicao.IdCartao,
            requisicao.Valor,
            requisicao.DiaOcorrencia);
    }

    private async Task<Guid> CadastrarAsync(Guid idUsuario, CadastrarDespesaRecorrenteRequest requisicao)
    {
        using HttpResponseMessage resposta = await _api.Client.PostAsJsonAsync(
            new Uri($"/api/usuarios/{idUsuario}/despesas-recorrentes", UriKind.Relative),
            requisicao);
        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        CadastrarDespesaRecorrenteResponse? corpo = await resposta.Content.ReadFromJsonAsync<CadastrarDespesaRecorrenteResponse>();
        return corpo!.Id;
    }

    private Task<HttpResponseMessage> ListarAsync(Guid idUsuario) =>
        _api.Client.GetAsync(new Uri($"/api/usuarios/{idUsuario}/despesas-recorrentes", UriKind.Relative));
}
