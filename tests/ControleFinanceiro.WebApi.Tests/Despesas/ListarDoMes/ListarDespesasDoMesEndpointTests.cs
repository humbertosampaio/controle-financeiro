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
using ControleFinanceiro.WebApi.Tests.Importacoes.ImportarFatura;
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
        // Com esse ciclo, a ocorrência no dia 5 fica no mês anterior ao do pagamento, antes da despesa no Pix.
        AdicionarCartaoRequest comCiclo = cartao with { DiaFechamento = 25, DiaVencimento = 10 };
        Guid idCartao = await _api.AdicionarCartaoAsync(idUsuario, comCiclo);
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
                Esperada(noCredito, ocorrenciaNoCredito, idNoCredito, mesReferencia.NoDia(comCiclo.DiaVencimento).Value),
                Esperada(noPix, ocorrenciaNoPix, idNoPix, ocorrenciaNoPix),
            ],
            opcoes => opcoes.Excluding(d => d.Id).WithStrictOrdering());
    }

    // O cartão da fatura vence no dia 10 e fecha no dia 3: a 3/10 de 15/07/2025 tem a primeira parcela em 08/2025.
    [Theory]
    [InlineApiAutoData(08, 2025, 1, 1)]
    [InlineApiAutoData(10, 2025, 3, 0)]
    public async Task Get_Quando_MesDeUmaParcela_Deve_TrazerOsDadosDaParcelaDoMes(
        int mes,
        int ano,
        int numeroEsperado,
        int statusEsperado,
        CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await ImportarFaturaComParceladaAsync(cadastro);
        DespesaParcelada parcelada = (await _api.ObterParceladasAsync()).Single();
        ParcelaDespesa parcelaDoMes = parcelada.Parcelas.Single(p => p.NumeroParcela == numeroEsperado);

        using HttpResponseMessage resposta = await ListarAsync(idUsuario, ano, mes);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        ListarDespesasDoMesResponse? corpo = await resposta.Content.ReadFromJsonAsync<ListarDespesasDoMesResponse>();
        ListarDespesasDoMesResponse.Despesa despesa = corpo!.Despesas.Should().ContainSingle().Subject;
        despesa.Id.Should().Be(parcelada.Id.Value);
        despesa.Parcela.Should().Be(new ListarDespesasDoMesResponse.ParcelaDoMes(parcelaDoMes.Id.Value, numeroEsperado, Total: 10));
        despesa.IdCartao.Should().Be(parcelada.IdCartao.Value);
        despesa.FormaPagamento.Should().Be(FormaPagamento.Credito.Value);
        despesa.Valor.Should().Be(99.90m);
        despesa.StatusPagamento.Should().Be(statusEsperado);
        despesa.DataPagamentoProjetada.Should().Be(new DateOnly(ano, mes, 10));
        despesa.DataPagamentoEfetiva.Should().Be(parcelaDoMes.Pagamento.DataEfetiva);
    }

    [Theory, ApiAutoData]
    public async Task Get_Quando_MesForaDasParcelas_Deve_NaoTrazerADespesa(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await ImportarFaturaComParceladaAsync(cadastro);

        using HttpResponseMessage resposta = await ListarAsync(idUsuario, ano: 2026, mes: 6);

        ListarDespesasDoMesResponse? corpo = await resposta.Content.ReadFromJsonAsync<ListarDespesasDoMesResponse>();
        corpo!.Despesas.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_Quando_RequisicaoInvalida_Deve_RetornarErroEmCadaCampo()
    {
        using HttpResponseMessage resposta = await ListarAsync(Guid.Empty, ano: 0, mes: 13);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.CamposComErroAsync()).Should().BeEquivalentTo("idUsuario", "ano", "mes");
    }

    private async Task<Guid> ImportarFaturaComParceladaAsync(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await _api.ImportarFaturaC6Async(idUsuario);

        return idUsuario;
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
            DataPagamentoEfetiva: null,
            Parcela: null);
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
