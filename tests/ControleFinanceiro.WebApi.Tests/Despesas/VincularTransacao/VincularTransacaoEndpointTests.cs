using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.WebApi.Endpoints.Beneficiarios.CadastrarLote;
using ControleFinanceiro.WebApi.Endpoints.Despesas.CadastrarRecorrente;
using ControleFinanceiro.WebApi.Endpoints.Despesas.GerarDoMes;
using ControleFinanceiro.WebApi.Endpoints.Despesas.VincularTransacao;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Importacoes.ImportarExtrato;
using ControleFinanceiro.WebApi.Tests.Importacoes.ImportarFatura;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;
using System.Net.Http.Json;

namespace ControleFinanceiro.WebApi.Tests.Despesas.VincularTransacao;

[Collection(ApiCollection.Nome)]
public sealed class VincularTransacaoEndpointTests : IAsyncLifetime
{
    private const string PixEnviado = VinculoExtensions.PixEnviado;
    private const string PixRecebido = "Pix recebido";
    private const string BoletoPago = "Pagamento de boleto";
    private const string CompraAVista = VinculoExtensions.CompraAVista;
    private const string CompraParcelada = VinculoExtensions.CompraParcelada;
    private const string CompraInternacional = VinculoExtensions.CompraInternacional;
    private const string CompraRepetidaDoVirtual = VinculoExtensions.CompraRepetidaDoVirtual;

    private readonly ApiComPostgresFixture _api;

    public VincularTransacaoEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory, ApiAutoData]
    public async Task Post_Quando_SimplesESaidaDoExtrato_Deve_PagarComOValorDaTransacao(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        CadastrarDespesaRecorrenteRequest recorrente,
        GerarDespesasDoMesRequest geracao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await _api.ImportarExtratoC6Async(idUsuario);
        DespesaSimples despesa = await _api.GerarSimplesAsync(idUsuario, beneficiarios, recorrente with { Valor = 75.00m }, geracao);
        Transacao transacao = await _api.ObterTransacaoAsync(idUsuario, PixEnviado);

        using HttpResponseMessage resposta = await _api.VincularAsync(idUsuario, despesa.Id.Value, new VincularTransacaoRequest { IdTransacao = transacao.Id.Value });

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        VincularTransacaoResponse? corpo = await resposta.Content.ReadFromJsonAsync<VincularTransacaoResponse>();
        corpo.Should().Be(new VincularTransacaoResponse(
            despesa.Id.Value,
            IdParcela: null,
            NumeroParcela: null,
            ValorProjetado: 75.00m,
            ValorEfetivo: 89.90m,
            transacao.Data,
            ValorCobrado: null));
        Pagamento gravado = (await _api.ObterTodosAsync(db => db.Despesas.OfType<DespesaSimples>())).Single().Pagamento;
        gravado.Status.Should().Be(StatusPagamento.Pago);
        gravado.IdTransacao.Should().Be(transacao.Id);
        gravado.DataEfetiva.Should().Be(transacao.Data);
        gravado.ValorEfetivo.Should().Be((ReaisPositivo)89.90m);
        gravado.ValorProjetado.Should().Be((ReaisPositivo)75.00m);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_AdiadaECompraAVistaNoCartao_Deve_CobrarSemPagar(
        CadastrarUsuarioRequest cadastro,
        CadastrarDespesaRecorrenteRequest recorrente,
        GerarDespesasDoMesRequest geracao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        DespesaAdiada despesa = await _api.ImportarFaturaComPrevisaoAsync(idUsuario, CompraAVista, recorrente, geracao);
        Transacao transacao = await _api.ObterTransacaoAsync(idUsuario, CompraAVista);

        using HttpResponseMessage resposta = await _api.VincularAsync(idUsuario, despesa.Id.Value, new VincularTransacaoRequest { IdTransacao = transacao.Id.Value });

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        VincularTransacaoResponse? corpo = await resposta.Content.ReadFromJsonAsync<VincularTransacaoResponse>();
        corpo.Should().Be(new VincularTransacaoResponse(
            despesa.Id.Value,
            IdParcela: null,
            NumeroParcela: null,
            despesa.Pagamento.ValorProjetado,
            ValorEfetivo: null,
            DataEfetiva: null,
            ValorCobrado: 123.45m));
        DespesaAdiada gravada = (await _api.ObterTodosAsync(db => db.Despesas.OfType<DespesaAdiada>()))
            .Single(d => d.Id == despesa.Id);
        gravada.Cobranca.Should().Be(new Cobranca(transacao.Id, (ReaisPositivo)123.45m));
        gravada.Pagamento.Should().Be(despesa.Pagamento);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_AdiadaECompraDeOutroFinalDaMesmaFatura_Deve_Cobrar(
        CadastrarUsuarioRequest cadastro,
        CadastrarDespesaRecorrenteRequest recorrente,
        GerarDespesasDoMesRequest geracao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        DespesaAdiada despesa = await _api.ImportarFaturaComPrevisaoAsync(idUsuario, CompraRepetidaDoVirtual, recorrente, geracao);
        IReadOnlyList<DespesaAdiada> adiadas = await _api.ObterTodosAsync(db => db.Despesas.OfType<DespesaAdiada>());
        Transacao doOutroFinal = (await _api.ObterTodosAsync(db => db.Transacoes))
            .Single(t => t.Descricao == CompraRepetidaDoVirtual && adiadas.All(d => d.Cobranca?.IdTransacao != t.Id));

        using HttpResponseMessage resposta = await _api.VincularAsync(idUsuario, despesa.Id.Value, new VincularTransacaoRequest { IdTransacao = doOutroFinal.Id.Value });

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        doOutroFinal.Origem.IdCartao.Should().NotBe(despesa.IdCartao);
        DespesaAdiada gravada = (await _api.ObterTodosAsync(db => db.Despesas.OfType<DespesaAdiada>()))
            .Single(d => d.Id == despesa.Id);
        gravada.Cobranca.Should().Be(new Cobranca(doOutroFinal.Id, doOutroFinal.ValorAbsoluto));
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_AdiadaJaCobradaPorOutraCompra_Deve_RetornarConflito(
        CadastrarUsuarioRequest cadastro,
        CadastrarDespesaRecorrenteRequest recorrente,
        GerarDespesasDoMesRequest geracao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await _api.ImportarFaturaComPrevisaoAsync(idUsuario, CompraAVista, recorrente, geracao);
        DespesaAdiada cobrada = await _api.ObterAdiadaDaCompraAsync(idUsuario, CompraInternacional);
        Transacao livre = await _api.ObterTransacaoAsync(idUsuario, CompraAVista);

        using HttpResponseMessage resposta = await _api.VincularAsync(idUsuario, cobrada.Id.Value, new VincularTransacaoRequest { IdTransacao = livre.Id.Value });

        resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await _api.ObterTodosAsync(db => db.Despesas.OfType<DespesaAdiada>()))
            .Single(d => d.Id == cobrada.Id)
            .Cobranca.Should().Be(cobrada.Cobranca);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_ParcelaDaCompra_Deve_CobrarSoAParcelaDoMesmoNumeroSemPagar(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await ImportarFaturaAsync(cadastro);
        DespesaParcelada despesa = (await _api.ObterParceladasAsync()).Single();
        Transacao transacao = await _api.ObterTransacaoAsync(idUsuario, CompraParcelada);

        using HttpResponseMessage resposta = await _api.VincularAsync(idUsuario, despesa.Id.Value, new VincularTransacaoRequest { IdTransacao = transacao.Id.Value });

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        ParcelaDespesa terceira = despesa.Parcelas.Single(p => p.NumeroParcela == 3);
        VincularTransacaoResponse? corpo = await resposta.Content.ReadFromJsonAsync<VincularTransacaoResponse>();
        corpo.Should().Be(new VincularTransacaoResponse(
            despesa.Id.Value,
            terceira.Id.Value,
            NumeroParcela: 3,
            terceira.ValorProjetado,
            ValorEfetivo: null,
            DataEfetiva: null,
            ValorCobrado: 99.90m));
        DespesaParcelada gravada = (await _api.ObterParceladasAsync()).Single();
        gravada.Parcelas.Should().HaveCount(10);
        gravada.Parcelas.Single(p => p.TemCobranca).Cobranca.Should().Be(new Cobranca(transacao.Id, (ReaisPositivo)99.90m));
        Pagamento[] pendentes = despesa.Parcelas
            .OrderBy(p => p.NumeroParcela)
            .Select(p => p.Pagamento)
            .ToArray();
        gravada.Parcelas
            .OrderBy(p => p.NumeroParcela)
            .Select(p => p.Pagamento)
            .Should().Equal(pendentes);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_ParcelaDaFaturaSeguinte_Deve_CobrarAParcelaSeguinteNaMesmaDespesa(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await ImportarFaturaAsync(cadastro);
        await _api.ImportarFaturaComSucessoAsync(idUsuario, LayoutArquivoImportacao.C6CartaoCsv, FaturaCartaoC6.ComParcelaSeguinte());
        DespesaParcelada despesa = (await _api.ObterParceladasAsync()).Single();
        Transacao quarta = (await _api.ObterTodosAsync(db => db.Transacoes)).Single(t => t.Parcela == (Parcela)(4, 10));

        using HttpResponseMessage resposta = await _api.VincularAsync(idUsuario, despesa.Id.Value, new VincularTransacaoRequest { IdTransacao = quarta.Id.Value });

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        (await _api.ObterParceladasAsync()).Single().Parcelas
            .Single(p => p.TemCobranca)
            .NumeroParcela.Should().Be(4);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_FaturaReimportadaDepoisDoVinculo_Deve_ManterOVinculo(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await ImportarFaturaAsync(cadastro);
        DespesaParcelada despesa = (await _api.ObterParceladasAsync()).Single();
        Transacao transacao = await _api.ObterTransacaoAsync(idUsuario, CompraParcelada);
        await _api.VincularComSucessoAsync(idUsuario, despesa.Id.Value, transacao.Id.Value);

        await _api.ImportarFaturaComSucessoAsync(idUsuario, LayoutArquivoImportacao.C6CartaoCsv, FaturaCartaoC6.Completo());

        (await _api.ObterParceladasAsync()).Single().Parcelas
            .Single(p => p.NumeroParcela == 3)
            .Cobranca!.IdTransacao.Should().Be(transacao.Id);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_DespesaNaoExiste_Deve_RetornarNaoEncontrado(CadastrarUsuarioRequest cadastro, Guid idInexistente)
    {
        Guid idUsuario = await ImportarFaturaAsync(cadastro);
        Transacao transacao = await _api.ObterTransacaoAsync(idUsuario, CompraParcelada);

        using HttpResponseMessage resposta = await _api.VincularAsync(idUsuario, idInexistente, new VincularTransacaoRequest { IdTransacao = transacao.Id.Value });

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_TransacaoNaoExiste_Deve_RetornarNaoEncontrado(CadastrarUsuarioRequest cadastro, VincularTransacaoRequest requisicao)
    {
        Guid idUsuario = await ImportarFaturaAsync(cadastro);
        DespesaParcelada despesa = (await _api.ObterParceladasAsync()).Single();

        using HttpResponseMessage resposta = await _api.VincularAsync(idUsuario, despesa.Id.Value, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_TransacaoDeOutroUsuario_Deve_RetornarNaoEncontradoSemVincular(
        CadastrarUsuarioRequest cadastro,
        CadastrarUsuarioRequest outroCadastro)
    {
        Guid idOutro = await _api.CadastrarUsuarioAsync(outroCadastro);
        await _api.ImportarExtratoC6Async(idOutro);
        Transacao transacaoDoOutro = await _api.ObterTransacaoAsync(idOutro, PixEnviado);
        Guid idUsuario = await ImportarFaturaAsync(cadastro);
        DespesaParcelada despesa = (await _api.ObterParceladasAsync()).Single();

        using HttpResponseMessage resposta = await _api.VincularAsync(idUsuario, despesa.Id.Value, new VincularTransacaoRequest { IdTransacao = transacaoDoOutro.Id.Value });

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _api.ObterParceladasAsync())
            .Single()
            .Parcelas.Should().OnlyContain(p => p.Pagamento.IdTransacao == null);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_DespesaDeOutroUsuario_Deve_RetornarNaoEncontrado(
        CadastrarUsuarioRequest cadastro,
        CadastrarUsuarioRequest outroCadastro)
    {
        await ImportarFaturaAsync(outroCadastro);
        DespesaParcelada despesaDoOutro = (await _api.ObterParceladasAsync()).Single();
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await _api.ImportarExtratoC6Async(idUsuario);
        Transacao transacao = await _api.ObterTransacaoAsync(idUsuario, PixEnviado);

        using HttpResponseMessage resposta = await _api.VincularAsync(idUsuario, despesaDoOutro.Id.Value, new VincularTransacaoRequest { IdTransacao = transacao.Id.Value });

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_TransacaoJaVinculada_Deve_RetornarConflitoNaTransacao(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await ImportarFaturaAsync(cadastro);
        DespesaParcelada despesa = (await _api.ObterParceladasAsync()).Single();
        Transacao transacao = await _api.ObterTransacaoAsync(idUsuario, CompraParcelada);
        await _api.VincularComSucessoAsync(idUsuario, despesa.Id.Value, transacao.Id.Value);

        using HttpResponseMessage resposta = await _api.VincularAsync(idUsuario, despesa.Id.Value, new VincularTransacaoRequest { IdTransacao = transacao.Id.Value });

        resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await resposta.CamposComErroAsync()).Should().Equal("idTransacao");
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_TransacaoJaPagaDespesaInteira_Deve_RetornarConflitoEManterAOutraDespesa(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        CadastrarDespesaRecorrenteRequest recorrente,
        GerarDespesasDoMesRequest geracao)
    {
        Guid idUsuario = await ImportarFaturaAsync(cadastro);
        await _api.ImportarExtratoC6Async(idUsuario);
        DespesaSimples inteira = await _api.GerarSimplesAsync(idUsuario, beneficiarios, recorrente, geracao);
        DespesaParcelada outra = (await _api.ObterParceladasAsync()).Single();
        Transacao transacao = await _api.ObterTransacaoAsync(idUsuario, PixEnviado);
        await _api.VincularComSucessoAsync(idUsuario, inteira.Id.Value, transacao.Id.Value);

        using HttpResponseMessage resposta = await _api.VincularAsync(idUsuario, outra.Id.Value, new VincularTransacaoRequest { IdTransacao = transacao.Id.Value });

        resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await resposta.CamposComErroAsync()).Should().Equal("idTransacao");
        (await _api.ObterTodosAsync(db => db.Despesas.OfType<DespesaSimples>()))
            .Single()
            .Pagamento.IdTransacao.Should().Be(transacao.Id);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_DespesaJaPaga_Deve_RetornarConflitoNaTransacao(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        CadastrarDespesaRecorrenteRequest recorrente,
        GerarDespesasDoMesRequest geracao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await _api.ImportarExtratoC6Async(idUsuario);
        DespesaSimples despesa = await _api.GerarSimplesAsync(idUsuario, beneficiarios, recorrente, geracao);
        Transacao primeira = await _api.ObterTransacaoAsync(idUsuario, PixEnviado);
        Transacao segunda = await _api.ObterTransacaoAsync(idUsuario, BoletoPago);
        await _api.VincularComSucessoAsync(idUsuario, despesa.Id.Value, primeira.Id.Value);

        using HttpResponseMessage resposta = await _api.VincularAsync(idUsuario, despesa.Id.Value, new VincularTransacaoRequest { IdTransacao = segunda.Id.Value });

        resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await resposta.CamposComErroAsync()).Should().Equal("idTransacao");
        (await _api.ObterTodosAsync(db => db.Despesas.OfType<DespesaSimples>()))
            .Single()
            .Pagamento.IdTransacao.Should().Be(primeira.Id);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_TransacaoEhEntrada_Deve_RetornarErroNaTransacaoSemVincular(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        CadastrarDespesaRecorrenteRequest recorrente,
        GerarDespesasDoMesRequest geracao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await _api.ImportarExtratoC6Async(idUsuario);
        DespesaSimples despesa = await _api.GerarSimplesAsync(idUsuario, beneficiarios, recorrente, geracao);
        Transacao entrada = await _api.ObterTransacaoAsync(idUsuario, PixRecebido);

        using HttpResponseMessage resposta = await _api.VincularAsync(idUsuario, despesa.Id.Value, new VincularTransacaoRequest { IdTransacao = entrada.Id.Value });

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await resposta.CamposComErroAsync()).Should().Equal("idTransacao");
        (await _api.ObterTodosAsync(db => db.Despesas.OfType<DespesaSimples>()))
            .Single()
            .Pagamento.EstaPago.Should().BeFalse();
    }

    [Fact]
    public async Task Post_Quando_RequisicaoInvalida_Deve_RetornarErroEmCadaCampo()
    {
        using HttpResponseMessage resposta = await _api.VincularAsync(Guid.Empty, Guid.Empty, new VincularTransacaoRequest { IdTransacao = Guid.Empty });

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.CamposComErroAsync()).Should().BeEquivalentTo("idUsuario", "idDespesa", "idTransacao");
    }

    private async Task<Guid> ImportarFaturaAsync(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await _api.ImportarFaturaC6Async(idUsuario);

        return idUsuario;
    }
}
