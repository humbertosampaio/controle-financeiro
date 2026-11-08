using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.WebApi.Endpoints.Beneficiarios.CadastrarLote;
using ControleFinanceiro.WebApi.Endpoints.Despesas.CadastrarRecorrente;
using ControleFinanceiro.WebApi.Endpoints.Despesas.GerarDoMes;
using ControleFinanceiro.WebApi.Endpoints.Despesas.PagarFatura;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Despesas.PagarFatura;
using ControleFinanceiro.WebApi.Tests.Despesas.VincularTransacao;
using ControleFinanceiro.WebApi.Tests.Importacoes.ImportarExtrato;
using ControleFinanceiro.WebApi.Tests.Importacoes.ImportarFatura;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;

namespace ControleFinanceiro.WebApi.Tests.Despesas.DesvincularTransacao;

[Collection(ApiCollection.Nome)]
public sealed class DesvincularTransacaoEndpointTests : IAsyncLifetime
{
    private readonly ApiComPostgresFixture _api;

    public DesvincularTransacaoEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory, ApiAutoData]
    public async Task Delete_Quando_SimplesVinculada_Deve_VoltarParaPendenteSemOsEfetivos(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        CadastrarDespesaRecorrenteRequest recorrente,
        GerarDespesasDoMesRequest geracao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await _api.ImportarExtratoC6Async(idUsuario);
        DespesaSimples despesa = await _api.GerarSimplesAsync(idUsuario, beneficiarios, recorrente, geracao);
        Transacao transacao = await _api.ObterTransacaoAsync(idUsuario, VinculoExtensions.PixEnviado);
        await _api.VincularComSucessoAsync(idUsuario, despesa.Id.Value, transacao.Id.Value);

        using HttpResponseMessage resposta = await DesvincularAsync(idUsuario, despesa.Id.Value, transacao.Id.Value);

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        Pagamento gravado = (await _api.ObterTodosAsync(db => db.Despesas.OfType<DespesaSimples>())).Single().Pagamento;
        gravado.Should().Be(despesa.Pagamento);
    }

    [Theory, ApiAutoData]
    public async Task Delete_Quando_ParcelaCobrada_Deve_TirarSoACobrancaDela(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await _api.ImportarFaturaC6Async(idUsuario);
        DespesaParcelada despesa = (await _api.ObterParceladasAsync()).Single();
        Transacao transacao = await _api.ObterTransacaoAsync(idUsuario, VinculoExtensions.CompraParcelada);
        await _api.VincularComSucessoAsync(idUsuario, despesa.Id.Value, transacao.Id.Value);

        using HttpResponseMessage resposta = await DesvincularAsync(idUsuario, despesa.Id.Value, transacao.Id.Value);

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _api.ObterParceladasAsync()).Single().Parcelas.Should().BeEquivalentTo(despesa.Parcelas);
    }

    [Theory, ApiAutoData]
    public async Task Delete_Quando_AdiadaCobrada_Deve_TirarACobranca(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        CadastrarDespesaRecorrenteRequest recorrente,
        GerarDespesasDoMesRequest geracao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await _api.ImportarFaturaC6Async(idUsuario);
        DespesaAdiada despesa = await _api.GerarAdiadaAsync(idUsuario, beneficiarios, recorrente, geracao);
        Transacao transacao = await _api.ObterTransacaoAsync(idUsuario, VinculoExtensions.CompraAVista);
        await _api.VincularComSucessoAsync(idUsuario, despesa.Id.Value, transacao.Id.Value);

        using HttpResponseMessage resposta = await DesvincularAsync(idUsuario, despesa.Id.Value, transacao.Id.Value);

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        DespesaAdiada gravada = (await _api.ObterTodosAsync(db => db.Despesas.OfType<DespesaAdiada>())).Single();
        gravada.Cobranca.Should().BeNull();
        gravada.Pagamento.Should().Be(despesa.Pagamento);
    }

    [Theory, ApiAutoData]
    public async Task Delete_Quando_CompraNaoCobrouADespesa_Deve_RetornarNaoEncontrado(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await _api.ImportarFaturaC6Async(idUsuario);
        DespesaParcelada despesa = (await _api.ObterParceladasAsync()).Single();
        Transacao transacao = await _api.ObterTransacaoAsync(idUsuario, VinculoExtensions.CompraParcelada);

        using HttpResponseMessage resposta = await DesvincularAsync(idUsuario, despesa.Id.Value, transacao.Id.Value);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory, ApiAutoData]
    public async Task Delete_Quando_SaidaNaoPagouADespesa_Deve_RetornarNaoEncontrado(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        CadastrarDespesaRecorrenteRequest recorrente,
        GerarDespesasDoMesRequest geracao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await _api.ImportarExtratoC6Async(idUsuario);
        DespesaSimples despesa = await _api.GerarSimplesAsync(idUsuario, beneficiarios, recorrente, geracao);
        Transacao transacao = await _api.ObterTransacaoAsync(idUsuario, VinculoExtensions.PixEnviado);

        using HttpResponseMessage resposta = await DesvincularAsync(idUsuario, despesa.Id.Value, transacao.Id.Value);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory, ApiAutoData]
    public async Task Delete_Quando_TransacaoNaoExiste_Deve_RetornarNaoEncontrado(CadastrarUsuarioRequest cadastro, Guid idTransacao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await _api.ImportarFaturaC6Async(idUsuario);
        DespesaParcelada despesa = (await _api.ObterParceladasAsync()).Single();

        using HttpResponseMessage resposta = await DesvincularAsync(idUsuario, despesa.Id.Value, idTransacao);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory, ApiAutoData]
    public async Task Delete_Quando_DespesaNaoExiste_Deve_RetornarNaoEncontrado(CadastrarUsuarioRequest cadastro, Guid idDespesa, Guid idTransacao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await DesvincularAsync(idUsuario, idDespesa, idTransacao);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory, ApiAutoData]
    public async Task Delete_Quando_DespesaDeOutroUsuario_Deve_RetornarNaoEncontradoSemDesfazer(
        CadastrarUsuarioRequest cadastro,
        CadastrarUsuarioRequest outroCadastro)
    {
        Guid idOutro = await _api.CadastrarUsuarioAsync(outroCadastro);
        await _api.ImportarFaturaC6Async(idOutro);
        DespesaParcelada despesaDoOutro = (await _api.ObterParceladasAsync()).Single();
        Transacao transacaoDoOutro = await _api.ObterTransacaoAsync(idOutro, VinculoExtensions.CompraParcelada);
        await _api.VincularComSucessoAsync(idOutro, despesaDoOutro.Id.Value, transacaoDoOutro.Id.Value);
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await DesvincularAsync(idUsuario, despesaDoOutro.Id.Value, transacaoDoOutro.Id.Value);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _api.ObterParceladasAsync())
            .Single()
            .Parcelas.Single(p => p.NumeroParcela == 3)
            .Cobranca!.IdTransacao.Should().Be(transacaoDoOutro.Id);
    }

    [Theory, ApiAutoData]
    public async Task Delete_Quando_AdiadaPagaPelaFatura_Deve_RetornarConflitoEManterACobranca(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        CadastrarDespesaRecorrenteRequest recorrente,
        GerarDespesasDoMesRequest geracao,
        PagarFaturaRequest requisicao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await _api.ImportarExtratoC6Async(idUsuario);
        await _api.ImportarFaturaC6Async(idUsuario);
        DespesaAdiada despesa = await _api.GerarAdiadaAsync(idUsuario, beneficiarios, recorrente, geracao);
        Transacao compra = await _api.ObterTransacaoAsync(idUsuario, VinculoExtensions.CompraAVista);
        await _api.VincularComSucessoAsync(idUsuario, despesa.Id.Value, compra.Id.Value);
        Transacao saida = await _api.ObterTransacaoAsync(idUsuario, VinculoExtensions.PixEnviado);
        await _api.PagarFaturaComSucessoAsync(idUsuario, requisicao.Pagando(saida, despesa.IdCartao, despesa.MesReferencia));

        using HttpResponseMessage resposta = await DesvincularAsync(idUsuario, despesa.Id.Value, compra.Id.Value);

        resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await _api.ObterTodosAsync(db => db.Despesas.OfType<DespesaAdiada>())).Single().Cobranca!.IdTransacao.Should().Be(compra.Id);
    }

    [Fact]
    public async Task Delete_Quando_RequisicaoInvalida_Deve_RetornarErroEmCadaCampo()
    {
        using HttpResponseMessage resposta = await DesvincularAsync(Guid.Empty, Guid.Empty, Guid.Empty);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.CamposComErroAsync()).Should().BeEquivalentTo("idUsuario", "idDespesa", "idTransacao");
    }

    private Task<HttpResponseMessage> DesvincularAsync(Guid idUsuario, Guid idDespesa, Guid idTransacao)
    {
        return _api.Client.DeleteAsync(new Uri($"/api/usuarios/{idUsuario}/despesas/{idDespesa}/vinculos/{idTransacao}", UriKind.Relative));
    }
}
