using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.WebApi.Endpoints.Beneficiarios.CadastrarLote;
using ControleFinanceiro.WebApi.Endpoints.Despesas.CadastrarRecorrente;
using ControleFinanceiro.WebApi.Endpoints.Despesas.GerarDoMes;
using ControleFinanceiro.WebApi.Endpoints.Despesas.ListarDoMes;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.AdicionarCartao;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;
using System.Net.Http.Json;

namespace ControleFinanceiro.WebApi.Tests.Despesas.ListarDoMes;

[Collection(ApiCollection.Nome)]
public sealed class ListarDespesasDoMesEndpointTests : IAsyncLifetime
{
    private readonly ApiComPostgresFixture _api;

    public ListarDespesasDoMesEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory, ApiAutoData]
    public async Task Get_Quando_DespesasEmVariosMeses_Deve_ListarSoAsDoMesDoUsuarioPorDataDeOcorrencia(
        MesReferencia mesReferencia,
        MesReferencia outroMes,
        CadastrarUsuarioRequest cadastro,
        CadastrarUsuarioRequest outroCadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        AdicionarCartaoRequest cartao,
        CadastrarDespesaRecorrenteRequest recorrente)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        CadastrarDespesaRecorrenteRequest doUsuario = await ComItensDoUsuarioAsync(idUsuario, beneficiarios, recorrente);
        Guid idCartao = await _api.AdicionarCartaoAsync(idUsuario, cartao);
        CadastrarDespesaRecorrenteRequest noPix = doUsuario with
        {
            Descricao = "Aluguel",
            FormaPagamento = FormaPagamento.Pix.Value,
            IdCartao = null,
            DiaOcorrencia = 20,
        };
        CadastrarDespesaRecorrenteRequest noCredito = doUsuario with
        {
            Descricao = "Streaming",
            FormaPagamento = FormaPagamento.Credito.Value,
            IdCartao = idCartao,
            DiaOcorrencia = 5,
        };
        Guid idNoPix = await _api.CadastrarDespesaRecorrenteAsync(idUsuario, noPix);
        Guid idNoCredito = await _api.CadastrarDespesaRecorrenteAsync(idUsuario, noCredito);
        await GerarAsync(idUsuario, mesReferencia);
        await GerarAsync(idUsuario, outroMes);
        Guid idOutroUsuario = await _api.CadastrarUsuarioAsync(outroCadastro);
        await _api.CadastrarDespesaRecorrenteAsync(
            idOutroUsuario,
            await ComItensDoUsuarioAsync(idOutroUsuario, beneficiarios, noPix));
        await GerarAsync(idOutroUsuario, mesReferencia);

        using HttpResponseMessage resposta = await ListarAsync(idUsuario, mesReferencia.Ano, mesReferencia.Mes);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        ListarDespesasDoMesResponse? corpo = await resposta.Content.ReadFromJsonAsync<ListarDespesasDoMesResponse>();
        DateOnly ocorrenciaNoCredito = mesReferencia.Anterior().NoDia(noCredito.DiaOcorrencia).Value;
        DateOnly ocorrenciaNoPix = mesReferencia.NoDia(noPix.DiaOcorrencia).Value;
        corpo!.Despesas.Should().BeEquivalentTo(
            [
                Esperada(noCredito, ocorrenciaNoCredito, idNoCredito, mesReferencia.NoDia(cartao.DiaVencimento).Value),
                Esperada(noPix, ocorrenciaNoPix, idNoPix, ocorrenciaNoPix),
            ],
            opcoes => opcoes.Excluding(d => d.Id).WithStrictOrdering());
    }

    [Fact]
    public async Task Get_Quando_RequisicaoInvalida_Deve_RetornarErroEmCadaCampo()
    {
        using HttpResponseMessage resposta = await ListarAsync(Guid.Empty, ano: 0, mes: 13);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.CamposComErroAsync()).Should().BeEquivalentTo("idUsuario", "ano", "mes");
    }

    /// <summary>A recorrente com um beneficiário cadastrado e a categoria Outros do usuário.</summary>
    private async Task<CadastrarDespesaRecorrenteRequest> ComItensDoUsuarioAsync(
        Guid idUsuario,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        CadastrarDespesaRecorrenteRequest recorrente)
    {
        IReadOnlyList<Guid> idsBeneficiarios = await _api.CadastrarBeneficiariosAsync(idUsuario, beneficiarios);
        Guid idOutros = (await _api.ObterTodosAsync(db => db.Categorias))
            .Single(c => c.IdUsuario == IdUsuario.From(idUsuario))
            .Id.Value;

        return recorrente with { IdBeneficiario = idsBeneficiarios[0], IdCategoria = idOutros };
    }

    /// <summary>A despesa pendente que a recorrente gera, com o id da despesa vazio: o teste não o confere.</summary>
    private static ListarDespesasDoMesResponse.Despesa Esperada(
        CadastrarDespesaRecorrenteRequest recorrente,
        DateOnly dataOcorrencia,
        Guid idDespesaRecorrente,
        DateOnly dataPagamentoProjetada)
    {
        return new(
            Id: Guid.Empty,
            recorrente.IdBeneficiario,
            recorrente.Descricao!,
            recorrente.IdCategoria,
            dataOcorrencia,
            recorrente.FormaPagamento,
            recorrente.IdCartao,
            idDespesaRecorrente,
            recorrente.Valor,
            StatusPagamento.Pendente.Value,
            dataPagamentoProjetada,
            DataPagamentoEfetiva: null);
    }

    private async Task GerarAsync(Guid idUsuario, MesReferencia mesReferencia)
    {
        using HttpResponseMessage resposta = await _api.Client.PostAsJsonAsync(
            new Uri($"/api/usuarios/{idUsuario}/despesas/geracoes", UriKind.Relative),
            new GerarDespesasDoMesRequest { Ano = mesReferencia.Ano, Mes = mesReferencia.Mes });
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private Task<HttpResponseMessage> ListarAsync(Guid idUsuario, int ano, int mes) =>
        _api.Client.GetAsync(new Uri($"/api/usuarios/{idUsuario}/despesas?ano={ano}&mes={mes}", UriKind.Relative));
}
