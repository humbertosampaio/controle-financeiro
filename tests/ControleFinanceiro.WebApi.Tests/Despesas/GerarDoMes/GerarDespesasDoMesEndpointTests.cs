using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.WebApi.Endpoints.Beneficiarios.CadastrarLote;
using ControleFinanceiro.WebApi.Endpoints.Despesas.CadastrarRecorrente;
using ControleFinanceiro.WebApi.Endpoints.Despesas.GerarDoMes;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.AdicionarCartao;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Importacoes.ImportarFatura;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;
using System.Net.Http.Json;

namespace ControleFinanceiro.WebApi.Tests.Despesas.GerarDoMes;

[Collection(ApiCollection.Nome)]
public sealed class GerarDespesasDoMesEndpointTests : IAsyncLifetime
{
    private readonly ApiComPostgresFixture _api;

    public GerarDespesasDoMesEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory, ApiAutoData]
    public async Task Post_Quando_RecorrentesSemDespesaNoMes_Deve_GravarUmaDespesaPorRecorrente(
        [Frozen] MesReferencia mesReferencia,
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        AdicionarCartaoRequest cartao,
        CadastrarDespesaRecorrenteRequest recorrente,
        GerarDespesasDoMesRequest requisicao)
    {
        Recorrentes recorrentes = await CadastrarRecorrentesAsync(cadastro, beneficiarios, cartao, recorrente);

        using HttpResponseMessage resposta = await GerarAsync(recorrentes.IdUsuario, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        GerarDespesasDoMesResponse? corpo = await resposta.Content.ReadFromJsonAsync<GerarDespesasDoMesResponse>();
        corpo.Should().Be(new GerarDespesasDoMesResponse(Geradas: 2, JaExistentes: 0));
        List<Despesa> gravadas = await _api.ObterTodosAsync(db => db.Despesas);
        gravadas.Should().HaveCount(2).And.AllSatisfy(despesa =>
        {
            despesa.IdUsuario.Should().Be(IdUsuario.From(recorrentes.IdUsuario));
            despesa.Should().BeAssignableTo<DespesaInteira>()
                .Which.MesReferencia.Should().Be(mesReferencia);
        });
        DespesaSimples noPix = gravadas.OfType<DespesaSimples>().Should().ContainSingle().Subject;
        noPix.IdDespesaRecorrente.Should().Be(IdDespesaRecorrente.From(recorrentes.IdNoPix));
        noPix.Pagamento.Status.Should().Be(StatusPagamento.Pendente);
        DespesaAdiada noCredito = gravadas.OfType<DespesaAdiada>().Should().ContainSingle().Subject;
        noCredito.IdDespesaRecorrente.Should().Be(IdDespesaRecorrente.From(recorrentes.IdNoCredito));
        noCredito.IdCartao.Should().Be(IdCartao.From(recorrentes.IdCartao));
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_MesTemParcelaDeDespesaParcelada_Deve_GerarAsRecorrentes(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        AdicionarCartaoRequest cartao,
        CadastrarDespesaRecorrenteRequest recorrente)
    {
        Recorrentes recorrentes = await CadastrarRecorrentesAsync(cadastro, beneficiarios, cartao, recorrente);
        await _api.ImportarFaturaC6Async(recorrentes.IdUsuario);

        // A compra parcelada da fatura tem a primeira parcela em 08/2025.
        using HttpResponseMessage resposta = await GerarAsync(recorrentes.IdUsuario, new GerarDespesasDoMesRequest { Ano = 2025, Mes = 8 });

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        GerarDespesasDoMesResponse? corpo = await resposta.Content.ReadFromJsonAsync<GerarDespesasDoMesResponse>();
        corpo.Should().Be(new GerarDespesasDoMesResponse(Geradas: 2, JaExistentes: 0));
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_RecorrenteNoCredito_Deve_PorAOcorrenciaPeloCicloDaFaturaDoCartao(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        AdicionarCartaoRequest cartao,
        CadastrarDespesaRecorrenteRequest recorrente)
    {
        Recorrentes recorrentes = await CadastrarRecorrentesAsync(
            cadastro,
            beneficiarios,
            cartao with { DiaFechamento = 25, DiaVencimento = 5 },
            recorrente with { DiaOcorrencia = 26 });

        using HttpResponseMessage resposta = await GerarAsync(recorrentes.IdUsuario, new GerarDespesasDoMesRequest { Mes = 4, Ano = 2026 });

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        List<Despesa> gravadas = await _api.ObterTodosAsync(db => db.Despesas);
        DespesaAdiada noCredito = gravadas.OfType<DespesaAdiada>().Should().ContainSingle().Subject;
        noCredito.DataOcorrencia.Should().Be(new DateOnly(2026, 2, 26));
        noCredito.MesReferencia.Should().Be((MesReferencia)(4, 2026));
        noCredito.Pagamento.DataProjetada.Should().Be(new DateOnly(2026, 4, 5));
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_MesJaGerado_Deve_ManterAsMesmasDespesas(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        AdicionarCartaoRequest cartao,
        CadastrarDespesaRecorrenteRequest recorrente,
        GerarDespesasDoMesRequest requisicao)
    {
        Recorrentes recorrentes = await CadastrarRecorrentesAsync(cadastro, beneficiarios, cartao, recorrente);
        using HttpResponseMessage primeira = await GerarAsync(recorrentes.IdUsuario, requisicao);
        primeira.StatusCode.Should().Be(HttpStatusCode.OK);
        List<IdDespesa> geradas = (await _api.ObterTodosAsync(db => db.Despesas)).ConvertAll(d => d.Id);

        using HttpResponseMessage resposta = await GerarAsync(recorrentes.IdUsuario, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        GerarDespesasDoMesResponse? corpo = await resposta.Content.ReadFromJsonAsync<GerarDespesasDoMesResponse>();
        corpo.Should().Be(new GerarDespesasDoMesResponse(Geradas: 0, JaExistentes: 2));
        (await _api.ObterTodosAsync(db => db.Despesas)).Select(d => d.Id).Should().BeEquivalentTo(geradas);
    }

    [Fact]
    public async Task Post_Quando_RequisicaoInvalida_Deve_RetornarErroEmCadaCampo()
    {
        GerarDespesasDoMesRequest invalida = new() { Ano = 0, Mes = 13 };

        using HttpResponseMessage resposta = await GerarAsync(Guid.Empty, invalida);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.CamposComErroAsync()).Should().BeEquivalentTo("idUsuario", "ano", "mes");
    }

    /// <summary>
    /// Cadastra o usuário com um beneficiário, um cartão e duas recorrentes, uma no Pix e outra no crédito. A categoria
    /// é a Outros, que nasce com o usuário.
    /// </summary>
    private async Task<Recorrentes> CadastrarRecorrentesAsync(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        AdicionarCartaoRequest cartao,
        CadastrarDespesaRecorrenteRequest recorrente)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        IReadOnlyList<Guid> idsBeneficiarios = await _api.CadastrarBeneficiariosAsync(idUsuario, beneficiarios);
        Guid idCartao = await _api.AdicionarCartaoAsync(idUsuario, cartao);
        Guid idOutros = (await _api.ObterTodosAsync(db => db.Categorias))
            .Single(c => c.IdUsuario == IdUsuario.From(idUsuario))
            .Id.Value;
        CadastrarDespesaRecorrenteRequest doUsuario = recorrente with
        {
            IdBeneficiario = idsBeneficiarios[0],
            IdCategoria = idOutros,
        };

        Guid idNoPix = await _api.CadastrarDespesaRecorrenteAsync(
            idUsuario,
            doUsuario with { Descricao = "Aluguel", FormaPagamento = FormaPagamento.Pix.Value, IdCartao = null });
        Guid idNoCredito = await _api.CadastrarDespesaRecorrenteAsync(
            idUsuario,
            doUsuario with { Descricao = "Streaming", FormaPagamento = FormaPagamento.Credito.Value, IdCartao = idCartao });

        return new Recorrentes(idUsuario, idCartao, idNoPix, idNoCredito);
    }

    private Task<HttpResponseMessage> GerarAsync(Guid idUsuario, GerarDespesasDoMesRequest requisicao)
    {
        return _api.Client.PostAsJsonAsync(
            new Uri($"/api/usuarios/{idUsuario}/despesas/geracoes", UriKind.Relative),
            requisicao);
    }

    private sealed record class Recorrentes(Guid IdUsuario, Guid IdCartao, Guid IdNoPix, Guid IdNoCredito);
}
