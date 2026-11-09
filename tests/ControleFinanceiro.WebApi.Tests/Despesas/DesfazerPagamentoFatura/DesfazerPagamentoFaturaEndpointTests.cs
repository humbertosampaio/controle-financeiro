using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.WebApi.Endpoints.Despesas.PagarFatura;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Despesas.PagarFatura;
using ControleFinanceiro.WebApi.Tests.Despesas.VincularTransacao;
using ControleFinanceiro.WebApi.Tests.Importacoes.ImportarExtrato;
using ControleFinanceiro.WebApi.Tests.Importacoes.ImportarFatura;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;

namespace ControleFinanceiro.WebApi.Tests.Despesas.DesfazerPagamentoFatura;

[Collection(ApiCollection.Nome)]
public sealed class DesfazerPagamentoFaturaEndpointTests : IAsyncLifetime
{
    private readonly ApiComPostgresFixture _api;

    public DesfazerPagamentoFaturaEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory, ApiAutoData]
    public async Task Delete_Quando_FaturaPaga_Deve_VoltarAsAdiadasAPendenteComACobranca(CadastrarUsuarioRequest cadastro, PagarFaturaRequest requisicao)
    {
        Guid idUsuario = await ImportarExtratoEFaturaAsync(cadastro);
        DespesaAdiada fisico = await _api.ObterAdiadaDaCompraAsync(idUsuario, VinculoExtensions.CompraAVista);
        List<DespesaAdiada> antes = await LerAdiadasAsync();
        Transacao saida = await _api.ObterTransacaoAsync(idUsuario, VinculoExtensions.PixEnviado);
        await _api.PagarFaturaComSucessoAsync(idUsuario, requisicao.Pagando(saida, fisico.IdCartao, fisico.MesReferencia));

        using HttpResponseMessage resposta = await DesfazerAsync(idUsuario, saida.Id.Value);

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        List<DespesaAdiada> gravadas = await LerAdiadasAsync();
        gravadas.Should().BeEquivalentTo(antes);
        gravadas.Single(d => d.Id == fisico.Id).Cobranca.Should().NotBeNull();
    }

    [Theory, ApiAutoData]
    public async Task Delete_Quando_ParcelaPaga_Deve_VoltarAParcelaAPendenteComACobranca(CadastrarUsuarioRequest cadastro, PagarFaturaRequest requisicao)
    {
        Guid idUsuario = await ImportarExtratoEFaturaAsync(cadastro);
        DespesaParcelada despesa = (await _api.ObterParceladasAsync()).Single();
        Transacao compra = await _api.ObterTransacaoAsync(idUsuario, VinculoExtensions.CompraParcelada);
        await _api.VincularComSucessoAsync(idUsuario, despesa.Id.Value, compra.Id.Value);
        DespesaParcelada cobrada = (await _api.ObterParceladasAsync()).Single();
        ParcelaDespesa terceira = cobrada.Parcelas.Single(p => p.NumeroParcela == 3);
        Transacao saida = await _api.ObterTransacaoAsync(idUsuario, VinculoExtensions.PixEnviado);
        await _api.PagarFaturaComSucessoAsync(idUsuario, requisicao.Pagando(saida, despesa.IdCartao, terceira.MesReferencia));

        using HttpResponseMessage resposta = await DesfazerAsync(idUsuario, saida.Id.Value);

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        IReadOnlyList<ParcelaDespesa> gravadas = (await _api.ObterParceladasAsync()).Single().Parcelas;
        gravadas.Should().BeEquivalentTo(cobrada.Parcelas);
        gravadas.Single(p => p.NumeroParcela == 3).Cobranca!.IdTransacao.Should().Be(compra.Id);
    }

    [Theory, ApiAutoData]
    public async Task Delete_Quando_SaidaNaoPagouFatura_Deve_RetornarNaoEncontrado(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await ImportarExtratoEFaturaAsync(cadastro);
        Transacao saida = await _api.ObterTransacaoAsync(idUsuario, VinculoExtensions.PixEnviado);

        using HttpResponseMessage resposta = await DesfazerAsync(idUsuario, saida.Id.Value);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory, ApiAutoData]
    public async Task Delete_Quando_TransacaoNaoExiste_Deve_RetornarNaoEncontrado(CadastrarUsuarioRequest cadastro, Guid idTransacao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await DesfazerAsync(idUsuario, idTransacao);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory, ApiAutoData]
    public async Task Delete_Quando_SaidaDeOutroUsuario_Deve_RetornarNaoEncontradoSemDesfazer(
        CadastrarUsuarioRequest cadastro,
        CadastrarUsuarioRequest outroCadastro,
        PagarFaturaRequest requisicao)
    {
        Guid idOutro = await ImportarExtratoEFaturaAsync(outroCadastro);
        DespesaAdiada fisico = await _api.ObterAdiadaDaCompraAsync(idOutro, VinculoExtensions.CompraAVista);
        Transacao saidaDoOutro = await _api.ObterTransacaoAsync(idOutro, VinculoExtensions.PixEnviado);
        await _api.PagarFaturaComSucessoAsync(idOutro, requisicao.Pagando(saidaDoOutro, fisico.IdCartao, fisico.MesReferencia));
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await DesfazerAsync(idUsuario, saidaDoOutro.Id.Value);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await LerAdiadasAsync())
            .Single(d => d.Id == fisico.Id)
            .Pagamento.IdTransacao.Should().Be(saidaDoOutro.Id);
    }

    [Fact]
    public async Task Delete_Quando_RequisicaoInvalida_Deve_RetornarErroEmCadaCampo()
    {
        using HttpResponseMessage resposta = await DesfazerAsync(Guid.Empty, Guid.Empty);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.CamposComErroAsync()).Should().BeEquivalentTo("idUsuario", "idTransacao");
    }

    private async Task<Guid> ImportarExtratoEFaturaAsync(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await _api.ImportarExtratoC6Async(idUsuario);
        await _api.ImportarFaturaC6Async(idUsuario);

        return idUsuario;
    }

    private Task<List<DespesaAdiada>> LerAdiadasAsync() => _api.ObterTodosAsync(db => db.Despesas.OfType<DespesaAdiada>());

    private Task<HttpResponseMessage> DesfazerAsync(Guid idUsuario, Guid idTransacao)
    {
        return _api.Client.DeleteAsync(new Uri($"/api/usuarios/{idUsuario}/despesas/pagamentos-fatura/{idTransacao}", UriKind.Relative));
    }
}
