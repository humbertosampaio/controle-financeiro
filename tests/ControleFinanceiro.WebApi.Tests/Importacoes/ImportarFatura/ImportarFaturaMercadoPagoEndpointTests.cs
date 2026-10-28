using ControleFinanceiro.Domain.Cartoes;
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
    public async Task Post_Quando_PrimeiraFatura_Deve_CriarOCartaoAImportacaoEAsTransacoes(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ResolverBeneficiariosAsync(idUsuario, FaturaCartaoMercadoPago.Completo());

        ImportarFaturaResponse corpo = await ImportarComSucessoAsync(idUsuario, FaturaCartaoMercadoPago.Completo());

        ImportarFaturaResponse.CartaoImportado linha = corpo.Cartoes.Should().ContainSingle().Subject;
        ((SufixoCartao)linha.Sufixo).Should().Be(FaturaCartaoMercadoPago.Sufixo);
        linha.CartaoNovo.Should().BeTrue();
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
        await ResolverBeneficiariosAsync(idUsuario, FaturaCartaoMercadoPago.Completo());

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
        await ResolverBeneficiariosAsync(idUsuario, FaturaCartaoMercadoPago.Completo());

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
        await ResolverBeneficiariosAsync(idUsuario, FaturaCartaoMercadoPago.Completo());
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
        await ResolverBeneficiariosAsync(idUsuario, FaturaCartaoMercadoPago.Completo());
        _ = await ImportarComSucessoAsync(idUsuario, FaturaCartaoMercadoPago.Completo());
        await ResolverBeneficiariosAsync(idUsuario, FaturaCartaoMercadoPago.ComParcelaSeguinte());

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
        await ResolverBeneficiariosAsync(idUsuario, FaturaCartaoMercadoPago.ComLinhasIguais());
        ImportarFaturaResponse primeira = await ImportarComSucessoAsync(idUsuario, FaturaCartaoMercadoPago.ComLinhasIguais());

        ImportarFaturaResponse segunda = await ImportarComSucessoAsync(idUsuario, FaturaCartaoMercadoPago.ComLinhasIguais());

        primeira.Cartoes.Should().ContainSingle().Which.Novas.Should().Be(FaturaCartaoMercadoPago.QuantidadeLancamentos);
        segunda.Cartoes.Should().ContainSingle().Which.Novas.Should().Be(0);
        (await _api.ObterTodosAsync(db => db.Transacoes)).Should().HaveCount(FaturaCartaoMercadoPago.QuantidadeLancamentos);
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
    public async Task Post_Deve_DeixarOsDocumentosEOsProdutosForaDoQueEGravado(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ResolverBeneficiariosAsync(idUsuario, FaturaCartaoMercadoPago.Completo());

        _ = await ImportarComSucessoAsync(idUsuario, FaturaCartaoMercadoPago.Completo());

        (await _api.ObterTodosAsync(db => db.Transacoes)).Should().AllSatisfy(t =>
        {
            string?[] textos = [t.Descricao, t.Contraparte, t.CategoriaBanco];
            textos.Should().NotContain(texto =>
                texto != null
                && FaturaCartaoMercadoPago.TextosProibidos.Any(p => texto.Contains(p, StringComparison.Ordinal)));
        });
    }

    private Task<ImportarFaturaResponse> ImportarComSucessoAsync(Guid idUsuario, byte[] arquivo) =>
        _api.ImportarFaturaComSucessoAsync(idUsuario, LayoutArquivoImportacao.MercadoPagoCartaoCsv, arquivo);

    private Task ResolverBeneficiariosAsync(Guid idUsuario, byte[] arquivo) =>
        _api.ResolverBeneficiariosDaFaturaAsync(idUsuario, LayoutArquivoImportacao.MercadoPagoCartaoCsv, arquivo);
}
