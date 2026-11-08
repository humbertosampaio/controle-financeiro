using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.WebApi.Endpoints.Beneficiarios.CadastrarLote;
using ControleFinanceiro.WebApi.Endpoints.Despesas.CadastrarRecorrente;
using ControleFinanceiro.WebApi.Endpoints.Despesas.GerarDoMes;
using ControleFinanceiro.WebApi.Endpoints.Despesas.PagarFatura;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Despesas.VincularTransacao;
using ControleFinanceiro.WebApi.Tests.Importacoes.ImportarExtrato;
using ControleFinanceiro.WebApi.Tests.Importacoes.ImportarFatura;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;
using System.Net.Http.Json;

namespace ControleFinanceiro.WebApi.Tests.Despesas.PagarFatura;

[Collection(ApiCollection.Nome)]
public sealed class PagarFaturaEndpointTests : IAsyncLifetime
{
    private const string PixEnviado = VinculoExtensions.PixEnviado;
    private const string PixRecebido = "Pix recebido";
    private const string CompraAVista = VinculoExtensions.CompraAVista;
    private const string CompraDoVirtual = "PADARIA EXEMPLO";

    private readonly ApiComPostgresFixture _api;

    public PagarFaturaEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory, ApiAutoData]
    public async Task Post_Quando_DoisFinaisDaMesmaFatura_Deve_PagarAsCobrancasDosDoisComADataDaSaida(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        CadastrarDespesaRecorrenteRequest recorrente,
        GerarDespesasDoMesRequest geracao,
        PagarFaturaRequest requisicao)
    {
        Guid idUsuario = await ImportarExtratoEFaturaAsync(cadastro);
        (DespesaAdiada fisico, DespesaAdiada @virtual) = await _api.GerarAdiadaEmCadaFinalAsync(idUsuario, beneficiarios, recorrente, geracao);
        await CobrarAsync(idUsuario, fisico, CompraAVista);
        await CobrarAsync(idUsuario, @virtual, CompraDoVirtual);
        Transacao saida = await _api.ObterTransacaoAsync(idUsuario, PixEnviado);

        using HttpResponseMessage resposta = await _api.PagarFaturaAsync(idUsuario, requisicao.Pagando(saida, fisico.IdCartao, fisico.MesReferencia));

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        PagarFaturaResponse? corpo = await resposta.Content.ReadFromJsonAsync<PagarFaturaResponse>();
        corpo!.Pagas.Should().BeEquivalentTo([
            new PagarFaturaResponse.DespesaPaga(fisico.Id.Value, ValorEfetivo: 123.45m, saida.Data, Parcela: null),
            new PagarFaturaResponse.DespesaPaga(@virtual.Id.Value, ValorEfetivo: 18.90m, saida.Data, Parcela: null),
        ]);
        IReadOnlyList<DespesaAdiada> gravadas = await _api.ObterTodosAsync(db => db.Despesas.OfType<DespesaAdiada>());
        gravadas.Select(d => d.Pagamento).Should().OnlyContain(p => p.Status == StatusPagamento.Pago && p.IdTransacao == saida.Id && p.DataEfetiva == saida.Data);
        gravadas.Single(d => d.Id == @virtual.Id).Pagamento.ValorEfetivo.Should().Be((ReaisPositivo)18.90m);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_ParcelaCobrada_Deve_PagarAParcelaDoMes(CadastrarUsuarioRequest cadastro, PagarFaturaRequest requisicao)
    {
        Guid idUsuario = await ImportarExtratoEFaturaAsync(cadastro);
        DespesaParcelada despesa = (await _api.ObterParceladasAsync()).Single();
        Transacao compra = await _api.ObterTransacaoAsync(idUsuario, VinculoExtensions.CompraParcelada);
        await _api.VincularComSucessoAsync(idUsuario, despesa.Id.Value, compra.Id.Value);
        ParcelaDespesa terceira = despesa.Parcelas.Single(p => p.NumeroParcela == 3);
        Transacao saida = await _api.ObterTransacaoAsync(idUsuario, PixEnviado);

        using HttpResponseMessage resposta = await _api.PagarFaturaAsync(idUsuario, requisicao.Pagando(saida, despesa.IdCartao, terceira.MesReferencia));

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        PagarFaturaResponse? corpo = await resposta.Content.ReadFromJsonAsync<PagarFaturaResponse>();
        corpo!.Pagas.Should().Equal(new PagarFaturaResponse.DespesaPaga(
            despesa.Id.Value,
            ValorEfetivo: 99.90m,
            saida.Data,
            new PagarFaturaResponse.ParcelaPaga(terceira.Id.Value, Numero: 3, Total: 10)));
        Pagamento gravado = (await _api.ObterParceladasAsync()).Single().Parcelas.Single(p => p.NumeroParcela == 3).Pagamento;
        gravado.IdTransacao.Should().Be(saida.Id);
        gravado.ValorEfetivo.Should().Be((ReaisPositivo)99.90m);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_SemCobrancaPendente_Deve_RetornarEntidadeNaoProcessavel(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        CadastrarDespesaRecorrenteRequest recorrente,
        GerarDespesasDoMesRequest geracao,
        PagarFaturaRequest requisicao)
    {
        Guid idUsuario = await ImportarExtratoEFaturaAsync(cadastro);
        (DespesaAdiada fisico, _) = await _api.GerarAdiadaEmCadaFinalAsync(idUsuario, beneficiarios, recorrente, geracao);
        Transacao saida = await _api.ObterTransacaoAsync(idUsuario, PixEnviado);

        using HttpResponseMessage resposta = await _api.PagarFaturaAsync(idUsuario, requisicao.Pagando(saida, fisico.IdCartao, fisico.MesReferencia));

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await resposta.CamposComErroAsync()).Should().Equal("idCartao");
        (await _api.ObterTodosAsync(db => db.Despesas.OfType<DespesaAdiada>())).Should().OnlyContain(d => !d.Pagamento.EstaPago);
    }

    [Theory]
    [InlineApiAutoData(PixRecebido)]
    [InlineApiAutoData(CompraAVista)]
    public async Task Post_Quando_TransacaoNaoEhSaidaDaConta_Deve_RetornarEntidadeNaoProcessavel(
        string descricao,
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        CadastrarDespesaRecorrenteRequest recorrente,
        GerarDespesasDoMesRequest geracao,
        PagarFaturaRequest requisicao)
    {
        Guid idUsuario = await ImportarExtratoEFaturaAsync(cadastro);
        (DespesaAdiada fisico, _) = await _api.GerarAdiadaEmCadaFinalAsync(idUsuario, beneficiarios, recorrente, geracao);
        await CobrarAsync(idUsuario, fisico, CompraAVista);
        Transacao transacao = await _api.ObterTransacaoAsync(idUsuario, descricao);

        using HttpResponseMessage resposta = await _api.PagarFaturaAsync(idUsuario, requisicao.Pagando(transacao, fisico.IdCartao, fisico.MesReferencia));

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await resposta.CamposComErroAsync()).Should().Equal("idTransacao");
        (await _api.ObterTodosAsync(db => db.Despesas.OfType<DespesaAdiada>())).Should().OnlyContain(d => !d.Pagamento.EstaPago);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_SaidaJaPagaUmaSimples_Deve_RetornarConflito(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        CadastrarDespesaRecorrenteRequest recorrente,
        GerarDespesasDoMesRequest geracao,
        CadastrarLoteBeneficiariosRequest beneficiariosDaSimples,
        CadastrarDespesaRecorrenteRequest recorrenteDaSimples,
        PagarFaturaRequest requisicao)
    {
        Guid idUsuario = await ImportarExtratoEFaturaAsync(cadastro);
        (DespesaAdiada fisico, _) = await _api.GerarAdiadaEmCadaFinalAsync(idUsuario, beneficiarios, recorrente, geracao);
        await CobrarAsync(idUsuario, fisico, CompraAVista);
        DespesaSimples simples = await _api.GerarSimplesAsync(idUsuario, beneficiariosDaSimples, recorrenteDaSimples, geracao);
        Transacao saida = await _api.ObterTransacaoAsync(idUsuario, PixEnviado);
        await _api.VincularComSucessoAsync(idUsuario, simples.Id.Value, saida.Id.Value);

        using HttpResponseMessage resposta = await _api.PagarFaturaAsync(idUsuario, requisicao.Pagando(saida, fisico.IdCartao, fisico.MesReferencia));

        resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await resposta.CamposComErroAsync()).Should().Equal("idTransacao");
        (await _api.ObterTodosAsync(db => db.Despesas.OfType<DespesaAdiada>())).Should().OnlyContain(d => !d.Pagamento.EstaPago);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_SaidaJaPagouOutroMes_Deve_RetornarConflito(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        CadastrarDespesaRecorrenteRequest recorrente,
        GerarDespesasDoMesRequest geracao,
        PagarFaturaRequest requisicao)
    {
        Guid idUsuario = await ImportarExtratoEFaturaAsync(cadastro);
        (DespesaAdiada fisico, _) = await _api.GerarAdiadaEmCadaFinalAsync(idUsuario, beneficiarios, recorrente, geracao);
        await CobrarAsync(idUsuario, fisico, CompraAVista);
        Transacao saida = await _api.ObterTransacaoAsync(idUsuario, PixEnviado);
        await _api.PagarFaturaComSucessoAsync(idUsuario, requisicao.Pagando(saida, fisico.IdCartao, fisico.MesReferencia));

        using HttpResponseMessage resposta = await _api.PagarFaturaAsync(idUsuario, requisicao.Pagando(saida, fisico.IdCartao, fisico.MesReferencia.Posterior()));

        resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await resposta.CamposComErroAsync()).Should().Equal("idTransacao");
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_TransacaoNaoExiste_Deve_RetornarNaoEncontrado(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        CadastrarDespesaRecorrenteRequest recorrente,
        GerarDespesasDoMesRequest geracao,
        PagarFaturaRequest requisicao)
    {
        Guid idUsuario = await ImportarExtratoEFaturaAsync(cadastro);
        (DespesaAdiada fisico, _) = await _api.GerarAdiadaEmCadaFinalAsync(idUsuario, beneficiarios, recorrente, geracao);

        using HttpResponseMessage resposta = await _api.PagarFaturaAsync(idUsuario, requisicao with { IdCartao = fisico.IdCartao.Value });

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_CartaoNaoExiste_Deve_RetornarNaoEncontrado(CadastrarUsuarioRequest cadastro, PagarFaturaRequest requisicao)
    {
        Guid idUsuario = await ImportarExtratoEFaturaAsync(cadastro);
        Transacao saida = await _api.ObterTransacaoAsync(idUsuario, PixEnviado);

        using HttpResponseMessage resposta = await _api.PagarFaturaAsync(idUsuario, requisicao with { IdTransacao = saida.Id.Value });

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_Quando_RequisicaoInvalida_Deve_RetornarErroEmCadaCampo()
    {
        PagarFaturaRequest invalida = new() { Ano = 0, Mes = 13 };

        using HttpResponseMessage resposta = await _api.PagarFaturaAsync(Guid.Empty, invalida);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.CamposComErroAsync()).Should().BeEquivalentTo("idUsuario", "idTransacao", "idCartao", "ano", "mes");
    }

    private async Task<Guid> ImportarExtratoEFaturaAsync(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await _api.ImportarExtratoC6Async(idUsuario);
        await _api.ImportarFaturaC6Async(idUsuario);

        return idUsuario;
    }

    private async Task CobrarAsync(Guid idUsuario, DespesaAdiada despesa, string descricaoCompra)
    {
        Transacao compra = await _api.ObterTransacaoAsync(idUsuario, descricaoCompra);
        await _api.VincularComSucessoAsync(idUsuario, despesa.Id.Value, compra.Id.Value);
    }
}
