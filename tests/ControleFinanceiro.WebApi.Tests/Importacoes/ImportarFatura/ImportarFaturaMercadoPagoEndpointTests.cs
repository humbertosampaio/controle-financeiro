using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarFatura;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;

namespace ControleFinanceiro.WebApi.Tests.Importacoes.ImportarFatura;

/// <summary>
/// A fatura do cartão do Mercado Pago pela rota da fatura, com o layout dela. O 404, o limite do formulário e o 400
/// são da rota e estão no <see cref="ImportarFaturaEndpointTests"/>.
/// </summary>
[Collection(ApiCollection.Nome)]
public sealed class ImportarFaturaMercadoPagoEndpointTests : IAsyncLifetime
{
    private readonly ApiComPostgresFixture _api;

    public ImportarFaturaMercadoPagoEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory, ApiAutoData]
    public async Task Post_Quando_CartaoEAliasesResolvidos_Deve_GravarAImportacaoEAsTransacoes(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ResolverPendenciasAsync(idUsuario, FaturaCartaoMercadoPago.Completo());

        ImportarFaturaResponse corpo = await ImportarComSucessoAsync(idUsuario, FaturaCartaoMercadoPago.Completo());

        ImportarFaturaResponse.CartaoImportado linha = corpo.Cartoes.Should().ContainSingle().Subject;
        ((SufixoCartao)linha.Sufixo).Should().Be(FaturaCartaoMercadoPago.Sufixo);
        linha.Lidas.Should().Be(FaturaCartaoMercadoPago.QuantidadeLancamentos);
        linha.Novas.Should().Be(FaturaCartaoMercadoPago.QuantidadeLancamentos);

        Cartao cartao = (await _api.ObterTodosAsync(db => db.Cartoes)).Should().ContainSingle().Subject;
        ((Guid)cartao.Id).Should().Be(linha.IdCartao);
        cartao.IdUsuario.Should().Be(IdUsuario.From(idUsuario));
        cartao.Banco.Should().BeSameAs(Banco.MercadoPago);
        cartao.Sufixo.Should().Be(FaturaCartaoMercadoPago.Sufixo);

        ImportacaoArquivo importacao = (await _api.ObterTodosAsync(db => db.Importacoes)).Should().ContainSingle().Subject;
        ((Guid)importacao.Id).Should().Be(linha.IdImportacao);
        importacao.Layout.Should().BeSameAs(LayoutArquivoImportacao.MercadoPagoCartaoCsv);
        importacao.Origem.Should().Be(OrigemTransacao.DoCartao(cartao.Id));
        importacao.Periodo.Should().Be((PeriodoArquivo)(FaturaCartaoMercadoPago.PrimeiraData, FaturaCartaoMercadoPago.UltimaData));

        List<Transacao> transacoes = await _api.ObterTodosAsync(db => db.Transacoes);
        transacoes.Should().HaveCount(FaturaCartaoMercadoPago.QuantidadeLancamentos).And.AllSatisfy(t =>
        {
            t.Origem.Should().Be(OrigemTransacao.DoCartao(cartao.Id));
            t.IdImportacao.Should().Be(importacao.Id);
        });
        transacoes.Sum(t => t.Valor).Should().Be(FaturaCartaoMercadoPago.SomaLancamentos);
    }

    [Theory, ApiAutoData]
    public async Task Post_Deve_GravarAParcelaComADataDaCompraOriginal(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ResolverPendenciasAsync(idUsuario, FaturaCartaoMercadoPago.Completo());

        _ = await ImportarComSucessoAsync(idUsuario, FaturaCartaoMercadoPago.Completo());

        List<Transacao> transacoes = await _api.ObterTodosAsync(db => db.Transacoes);
        Transacao parcelada = transacoes.Should().ContainSingle(t => t.EhParcelada).Subject;
        parcelada.Parcela.Should().Be((Parcela)(3, 10));
        parcelada.Data.Should().Be(FaturaCartaoMercadoPago.PrimeiraData);
    }

    [Theory, ApiAutoData]
    public async Task Post_Deve_GravarOPagamentoComoValorPositivo(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ResolverPendenciasAsync(idUsuario, FaturaCartaoMercadoPago.Completo());

        _ = await ImportarComSucessoAsync(idUsuario, FaturaCartaoMercadoPago.Completo());

        List<Transacao> transacoes = await _api.ObterTodosAsync(db => db.Transacoes);
        Transacao pagamento = transacoes.Should().ContainSingle(t => t.Valor > 0m).Subject;
        pagamento.Data.Should().Be(FaturaCartaoMercadoPago.UltimaData);
        pagamento.Valor.Should().Be((Reais)1429.36m);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_ReimportaAMesmaFatura_Deve_ManterAsTransacoes(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ResolverPendenciasAsync(idUsuario, FaturaCartaoMercadoPago.Completo());
        _ = await ImportarComSucessoAsync(idUsuario, FaturaCartaoMercadoPago.Completo());

        ImportarFaturaResponse segunda = await ImportarComSucessoAsync(idUsuario, FaturaCartaoMercadoPago.Completo());

        ImportarFaturaResponse.CartaoImportado linha = segunda.Cartoes.Should().ContainSingle().Subject;
        linha.Novas.Should().Be(0);
        linha.JaExistentes.Should().Be(FaturaCartaoMercadoPago.QuantidadeLancamentos);
        (await _api.ObterTodosAsync(db => db.Transacoes)).Should().HaveCount(FaturaCartaoMercadoPago.QuantidadeLancamentos);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_ParcelaSeguinteComOMesmoNumero_Deve_GravarUmaTransacaoParaCadaParcela(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ResolverPendenciasAsync(idUsuario, FaturaCartaoMercadoPago.Completo());
        _ = await ImportarComSucessoAsync(idUsuario, FaturaCartaoMercadoPago.Completo());
        await ResolverPendenciasAsync(idUsuario, FaturaCartaoMercadoPago.ComParcelaSeguinte());

        ImportarFaturaResponse corpo = await ImportarComSucessoAsync(idUsuario, FaturaCartaoMercadoPago.ComParcelaSeguinte());

        corpo.Cartoes.Should().ContainSingle().Which.Novas.Should().Be(1);
        List<Transacao> transacoes = await _api.ObterTodosAsync(db => db.Transacoes);
        transacoes.Should().HaveCount(FaturaCartaoMercadoPago.QuantidadeLancamentos + 1);
        transacoes
            .Where(t => t.EhParcelada)
            .Select(t => t.Parcela)
            .Should().BeEquivalentTo([(Parcela)(3, 10), (Parcela)(4, 10)]);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_DuasLinhasIguais_Deve_GravarDuasTransacoesSemDuplicarAoReimportar(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ResolverPendenciasAsync(idUsuario, FaturaCartaoMercadoPago.ComLinhasIguais());
        ImportarFaturaResponse primeira = await ImportarComSucessoAsync(idUsuario, FaturaCartaoMercadoPago.ComLinhasIguais());

        ImportarFaturaResponse segunda = await ImportarComSucessoAsync(idUsuario, FaturaCartaoMercadoPago.ComLinhasIguais());

        primeira.Cartoes.Should().ContainSingle().Which.Novas.Should().Be(FaturaCartaoMercadoPago.QuantidadeLancamentos);
        segunda.Cartoes.Should().ContainSingle().Which.Novas.Should().Be(0);
        (await _api.ObterTodosAsync(db => db.Transacoes)).Should().HaveCount(FaturaCartaoMercadoPago.QuantidadeLancamentos);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_FinalSemCartao_Deve_RetornarErroNoArquivoSemGravar(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await _api.ResolverBeneficiariosDaFaturaAsync(idUsuario, LayoutArquivoImportacao.MercadoPagoCartaoCsv, FaturaCartaoMercadoPago.Completo());

        using HttpResponseMessage resposta = await _api.ImportarFaturaAsync(
            idUsuario, LayoutArquivoImportacao.MercadoPagoCartaoCsv, FaturaCartaoMercadoPago.Completo());

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await resposta.CamposComErroAsync()).Should().Equal("arquivo");
        (await _api.ObterTodosAsync(db => db.Cartoes)).Should().BeEmpty();
        await _api.DeveEstarSemImportacaoAsync();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_ColunasDiferentesDoPrompt_Deve_RetornarErroNoArquivoSemGravar(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        using HttpResponseMessage resposta = await _api.ImportarFaturaAsync(
            idUsuario, LayoutArquivoImportacao.MercadoPagoCartaoCsv, FaturaCartaoMercadoPago.ComColunaRenomeada());

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await resposta.CamposComErroAsync()).Should().Equal("arquivo");
        await _api.DeveEstarSemImportacaoAsync();
    }

    [Theory, ApiAutoData]
    public async Task Post_Deve_DeixarOsProdutosForaDoQueEGravado(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ResolverPendenciasAsync(idUsuario, FaturaCartaoMercadoPago.Completo());

        _ = await ImportarComSucessoAsync(idUsuario, FaturaCartaoMercadoPago.Completo());

        (await _api.ObterTodosAsync(db => db.Transacoes)).Should().AllSatisfy(t =>
        {
            string?[] textos = [t.Descricao, t.Contraparte, t.CategoriaBanco];
            textos.Should().NotContain(texto =>
                texto != null
                && FaturaCartaoMercadoPago.TextosProibidos.Any(p => texto.Contains(p, StringComparison.Ordinal)));
        });
    }

    [Theory, ApiAutoData]
    public async Task Post_Deve_CriarADespesaParceladaSemCategoria(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ResolverPendenciasAsync(idUsuario, FaturaCartaoMercadoPago.Completo());

        ImportarFaturaResponse corpo = await ImportarComSucessoAsync(idUsuario, FaturaCartaoMercadoPago.Completo());

        corpo.Cartoes.Should().ContainSingle().Which.DespesasParceladasCriadas.Should().Be(1);
        DespesaParcelada despesa = (await _api.ObterParceladasAsync()).Should().ContainSingle().Subject;
        despesa.IdCategoria.Should().BeNull();
        despesa.DataOcorrencia.Should().Be(FaturaCartaoMercadoPago.PrimeiraData);
        despesa.Parcelas.Should().HaveCount(10).And.OnlyContain(p => p.Pagamento.ValorProjetado == (ReaisPositivo)100m);
        despesa.Parcelas.Single(p => p.NumeroParcela == 1).MesReferencia.Should().Be((MesReferencia)(12, 2025));
    }

    [Theory, ApiAutoData]
    public async Task Post_Deve_CriarAAdiadaDeCadaCompraAVistaSemCategoria(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ResolverPendenciasAsync(idUsuario, FaturaCartaoMercadoPago.Completo());

        ImportarFaturaResponse corpo = await ImportarComSucessoAsync(idUsuario, FaturaCartaoMercadoPago.Completo());

        corpo.Cartoes.Should().ContainSingle().Which.DespesasAdiadasCriadas.Should().Be(5);
        IReadOnlyList<Transacao> transacoes = await _api.ObterTodosAsync(db => db.Transacoes);
        IReadOnlyList<DespesaAdiada> adiadas = await _api.ObterTodosAsync(db => db.Despesas.OfType<DespesaAdiada>());
        adiadas.Should().OnlyContain(d => d.IdCategoria == null && !d.Pagamento.EstaPago);
        adiadas
            .Select(d => transacoes.Single(t => t.Id == d.Cobranca!.IdTransacao).ValorAbsoluto.Value)
            .Should()
            .BeEquivalentTo([25.90m, 1234.56m, 49.90m, 9.50m, 9.50m]);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_FaturaSeguinte_Deve_NaoDuplicarADespesa(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ResolverPendenciasAsync(idUsuario, FaturaCartaoMercadoPago.Completo());
        await ImportarComSucessoAsync(idUsuario, FaturaCartaoMercadoPago.Completo());

        await ImportarComSucessoAsync(idUsuario, FaturaCartaoMercadoPago.ComParcelaSeguinte());

        (await _api.ObterParceladasAsync()).Should().ContainSingle();
    }

    private Task<ImportarFaturaResponse> ImportarComSucessoAsync(Guid idUsuario, byte[] arquivo) =>
        _api.ImportarFaturaComSucessoAsync(idUsuario, LayoutArquivoImportacao.MercadoPagoCartaoCsv, arquivo);

    private async Task ResolverPendenciasAsync(Guid idUsuario, byte[] arquivo)
    {
        await _api.ResolverCartoesDaFaturaAsync(idUsuario, LayoutArquivoImportacao.MercadoPagoCartaoCsv, arquivo);
        await _api.ResolverBeneficiariosDaFaturaAsync(idUsuario, LayoutArquivoImportacao.MercadoPagoCartaoCsv, arquivo);
    }
}
