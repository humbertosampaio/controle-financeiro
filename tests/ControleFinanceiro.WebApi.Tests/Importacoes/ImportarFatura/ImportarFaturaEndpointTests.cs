using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarFatura;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.AdicionarCartao;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Importacoes.ImportarExtrato;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace ControleFinanceiro.WebApi.Tests.Importacoes.ImportarFatura;

[Collection(ApiCollection.Nome)]
public sealed class ImportarFaturaEndpointTests : IAsyncLifetime
{
    private readonly ApiComPostgresFixture _api;

    public ImportarFaturaEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory, ApiAutoData]
    public async Task Post_Quando_PrimeiraFatura_Deve_CriarOsCartoesUmaImportacaoPorCartaoEAsTransacoes(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, FaturaCartaoC6.Completo());

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        ImportarFaturaResponse corpo = (await resposta.Content.ReadFromJsonAsync<ImportarFaturaResponse>())!;
        corpo.Cartoes.Select(c => (SufixoCartao)c.Sufixo).Should().Equal(FaturaCartaoC6.SufixoFisico, FaturaCartaoC6.SufixoVirtual);
        corpo.Cartoes.Should().AllSatisfy(c =>
        {
            c.CartaoNovo.Should().BeTrue();
            c.VencimentoPadrao.Should().BeTrue();
            c.Lidas.Should().Be(FaturaCartaoC6.LancamentosPorCartao);
            c.Novas.Should().Be(FaturaCartaoC6.LancamentosPorCartao);
            c.JaExistentes.Should().Be(0);
        });

        List<Cartao> cartoes = await _api.ObterTodosAsync(db => db.Cartoes);
        cartoes.Select(c => c.Sufixo).Should().BeEquivalentTo([FaturaCartaoC6.SufixoFisico, FaturaCartaoC6.SufixoVirtual]);
        cartoes.Should().AllSatisfy(c =>
        {
            c.IdUsuario.Should().Be(IdUsuario.From(idUsuario));
            c.Banco.Should().BeSameAs(Banco.C6);
            c.DiaVencimento.Should().Be(DiaVencimentoCartao.Padrao);
        });

        Cartao fisico = cartoes.Single(c => c.Sufixo == FaturaCartaoC6.SufixoFisico);
        ImportarFaturaResponse.CartaoImportado linhaFisico = corpo.Cartoes[0];
        ((Guid)fisico.Id).Should().Be(linhaFisico.IdCartao);

        List<ImportacaoArquivo> importacoes = await _api.ObterTodosAsync(db => db.Importacoes);
        importacoes.Should().HaveCount(2);
        ImportacaoArquivo importacaoFisico = importacoes.Single(i => i.Origem == OrigemTransacao.DoCartao(fisico.Id));
        ((Guid)importacaoFisico.Id).Should().Be(linhaFisico.IdImportacao);
        importacaoFisico.Layout.Should().BeSameAs(LayoutArquivoImportacao.C6CartaoCsv);
        importacaoFisico.Periodo.Should().Be((PeriodoArquivo)(FaturaCartaoC6.PrimeiraCompraFisico, FaturaCartaoC6.UltimaCompraFisico));
        importacaoFisico.Lidas.Should().Be(FaturaCartaoC6.LancamentosPorCartao);

        List<Transacao> transacoes = await _api.ObterTodosAsync(db => db.Transacoes);
        transacoes.Should().HaveCount(FaturaCartaoC6.QuantidadeLancamentos);
        transacoes.Where(t => t.Origem == OrigemTransacao.DoCartao(fisico.Id)).Should().AllSatisfy(t =>
        {
            t.IdUsuario.Should().Be(IdUsuario.From(idUsuario));
            t.IdImportacao.Should().Be(importacaoFisico.Id);
        });
        transacoes.Sum(t => t.Valor).Should().Be(213.75m);
    }

    [Theory, ApiAutoData]
    public async Task Post_Deve_GravarAParcelaEACategoriaDoBanco(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.Completo());

        List<Transacao> transacoes = await _api.ObterTodosAsync(db => db.Transacoes);
        Transacao parcelada = transacoes.Should().ContainSingle(t => t.EhParcelada).Subject;
        parcelada.Parcela.Should().Be((Parcela)(3, 10));
        parcelada.Data.Should().Be(FaturaCartaoC6.PrimeiraCompraFisico);
        parcelada.CategoriaBanco.Should().Be("Eletrônicos");
        transacoes.Should().ContainSingle(t => t.CategoriaBanco == null);
    }

    [Theory, ApiAutoData]
    public async Task Post_Deve_DeixarONomeDoTitularForaDoQueEGravado(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.Completo());

        (await _api.ObterTodosAsync(db => db.Transacoes)).Should().AllSatisfy(t =>
        {
            t.Descricao.Value.Should().NotContain(FaturaCartaoC6.TitularFicticio);
            t.Contraparte.Should().BeNull();
            t.CategoriaBanco.Should().NotBe(FaturaCartaoC6.TitularFicticio);
        });
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_CartaoJaCadastrado_Deve_ReaproveitarOCartaoComOVencimentoOriginal(
        CadastrarUsuarioRequest cadastro,
        AdicionarCartaoRequest cartao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        AdicionarCartaoRequest fisico = cartao with { Banco = Banco.C6, Sufixo = FaturaCartaoC6.SufixoFisico, DiaVencimento = 15 };
        Guid idCartao = await _api.AdicionarCartaoAsync(idUsuario, fisico);

        ImportarFaturaResponse corpo = await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.Completo(), diaVencimento: 10);

        ImportarFaturaResponse.CartaoImportado linhaFisico = corpo.Cartoes[0];
        linhaFisico.IdCartao.Should().Be(idCartao);
        linhaFisico.CartaoNovo.Should().BeFalse();
        linhaFisico.VencimentoPadrao.Should().BeFalse();
        corpo.Cartoes[1].CartaoNovo.Should().BeTrue();

        List<Cartao> cartoes = await _api.ObterTodosAsync(db => db.Cartoes);
        cartoes.Should().HaveCount(2);
        cartoes.Single(c => c.Id == IdCartao.From(idCartao)).DiaVencimento.Should().Be((DiaVencimentoCartao)15);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_OutroUsuarioTemOMesmoCartao_Deve_CriarOCartaoDoUsuario(
        CadastrarUsuarioRequest cadastro,
        CadastrarUsuarioRequest cadastroOutro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        Guid idOutro = await _api.CadastrarUsuarioAsync(cadastroOutro);
        ImportarFaturaResponse doOutro = await ImportarComSucessoAsync(idOutro, FaturaCartaoC6.Completo());

        ImportarFaturaResponse corpo = await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.Completo());

        corpo.Cartoes.Should().AllSatisfy(c =>
        {
            c.CartaoNovo.Should().BeTrue();
            c.Novas.Should().Be(FaturaCartaoC6.LancamentosPorCartao);
        });
        List<Cartao> cartoes = await _api.ObterTodosAsync(db => db.Cartoes);
        cartoes.Where(c => c.IdUsuario == IdUsuario.From(idUsuario)).Select(c => (Guid)c.Id)
            .Should().BeEquivalentTo(corpo.Cartoes.Select(c => c.IdCartao));
        cartoes.Where(c => c.IdUsuario == IdUsuario.From(idOutro)).Select(c => (Guid)c.Id)
            .Should().BeEquivalentTo(doOutro.Cartoes.Select(c => c.IdCartao));
        (await _api.ObterTodosAsync(db => db.Transacoes)).Should().HaveCount(2 * FaturaCartaoC6.QuantidadeLancamentos);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_DiaUnicoEDiaDeUmFinal_Deve_AplicarOsDiasAosCartoesNovos(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        ImportarFaturaResponse corpo = await ImportarComSucessoAsync(
            idUsuario,
            FaturaCartaoC6.Completo(),
            diaVencimento: 10,
            vencimentos: $$"""{"{{FaturaCartaoC6.SufixoVirtual}}":20,"9999":5}""");

        corpo.Cartoes.Should().AllSatisfy(c => c.VencimentoPadrao.Should().BeFalse());
        List<Cartao> cartoes = await _api.ObterTodosAsync(db => db.Cartoes);
        cartoes.Should().HaveCount(2);
        cartoes.Single(c => c.Sufixo == FaturaCartaoC6.SufixoFisico).DiaVencimento.Should().Be((DiaVencimentoCartao)10);
        cartoes.Single(c => c.Sufixo == FaturaCartaoC6.SufixoVirtual).DiaVencimento.Should().Be((DiaVencimentoCartao)20);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_ReimportaAMesmaFatura_Deve_ManterAsTransacoesERegistrarAsNovasImportacoes(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        ImportarFaturaResponse primeira = await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.Completo());

        ImportarFaturaResponse segunda = await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.Completo());

        segunda.Cartoes.Should().AllSatisfy(c =>
        {
            c.CartaoNovo.Should().BeFalse();
            c.VencimentoPadrao.Should().BeFalse();
            c.Novas.Should().Be(0);
            c.JaExistentes.Should().Be(FaturaCartaoC6.LancamentosPorCartao);
        });
        segunda.Cartoes.Select(c => c.IdCartao).Should().Equal(primeira.Cartoes.Select(c => c.IdCartao));
        (await _api.ObterTodosAsync(db => db.Transacoes)).Should().HaveCount(FaturaCartaoC6.QuantidadeLancamentos);
        (await _api.ObterTodosAsync(db => db.Cartoes)).Should().HaveCount(2);
        (await _api.ObterTodosAsync(db => db.Importacoes)).Should().HaveCount(4);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_ParcelaSeguinteNaFaturaSeguinte_Deve_GravarUmaTransacaoParaCadaParcela(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.Completo());

        ImportarFaturaResponse corpo = await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.ComParcelaSeguinte());

        corpo.Cartoes[0].Novas.Should().Be(1);
        corpo.Cartoes[1].Novas.Should().Be(0);
        List<Transacao> transacoes = await _api.ObterTodosAsync(db => db.Transacoes);
        transacoes.Should().HaveCount(FaturaCartaoC6.QuantidadeLancamentos + 1);
        transacoes.Where(t => t.EhParcelada).Select(t => t.Parcela).Should().BeEquivalentTo([(Parcela)(3, 10), (Parcela)(4, 10)]);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_ArquivoDoExtrato_Deve_RetornarErroNoArquivoSemGravar(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, ExtratoContaC6.Completo());

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await resposta.CamposComErroAsync()).Should().Equal("arquivo");
        await _api.DeveEstarSemImportacaoAsync();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_UsuarioNaoExiste_Deve_RetornarNaoEncontradoSemGravar(Guid idUsuario)
    {
        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, FaturaCartaoC6.Completo());

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await _api.DeveEstarSemImportacaoAsync();
    }

    // A leitura do formulário para no limite da rota, antes do validator: o erro é geral, e não no campo do arquivo.
    [Theory, ApiAutoData]
    public async Task Post_Quando_FormularioAcimaDoLimiteDaRota_Deve_RecusarSemLerOArquivoInteiro(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, new byte[ImportarFaturaEndpoint.TamanhoMaximoFormulario + 1]);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.CamposComErroAsync()).Should().Equal("generalErrors");
        await _api.DeveEstarSemImportacaoAsync();
    }

    [Fact]
    public async Task Post_Quando_RequisicaoInvalida_Deve_RetornarErroEmCadaCampo()
    {
        using MultipartFormDataContent formulario = new()
        {
            { new StringContent("layout-desconhecido"), "layout" },
            { new StringContent("29"), "diaVencimento" },
            { new StringContent("""{"123":0}"""), "vencimentos" },
        };

        using HttpResponseMessage resposta = await _api.Client.PostAsync(ImportacaoFaturaExtensions.RotaFatura(Guid.Empty), formulario);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.CamposComErroAsync()).Should().BeEquivalentTo("idUsuario", "layout", "arquivo", "diaVencimento", "vencimentos");
        await _api.DeveEstarSemImportacaoAsync();
    }

    private Task<HttpResponseMessage> ImportarAsync(Guid idUsuario, byte[] arquivo)
    {
        return _api.ImportarFaturaAsync(idUsuario, LayoutArquivoImportacao.C6CartaoCsv, arquivo);
    }

    private Task<ImportarFaturaResponse> ImportarComSucessoAsync(
        Guid idUsuario,
        byte[] arquivo,
        int? diaVencimento = null,
        string? vencimentos = null)
    {
        return _api.ImportarFaturaComSucessoAsync(idUsuario, LayoutArquivoImportacao.C6CartaoCsv, arquivo, diaVencimento, vencimentos);
    }
}
