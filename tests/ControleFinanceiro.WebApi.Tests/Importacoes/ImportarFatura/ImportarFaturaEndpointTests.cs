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
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ControleFinanceiro.WebApi.Tests.Importacoes.ImportarFatura;

[Collection(ApiCollection.Nome)]
public sealed class ImportarFaturaEndpointTests : IAsyncLifetime
{
    private static readonly Uri _rotaCadastro = new("/api/usuarios", UriKind.Relative);

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
        Guid idUsuario = await CadastrarUsuarioAsync(cadastro);

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

        List<Cartao> cartoes = await LerCartoesAsync();
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

        List<ImportacaoArquivo> importacoes = await LerImportacoesAsync();
        importacoes.Should().HaveCount(2);
        ImportacaoArquivo importacaoFisico = importacoes.Single(i => i.Origem == OrigemTransacao.DoCartao(fisico.Id));
        ((Guid)importacaoFisico.Id).Should().Be(linhaFisico.IdImportacao);
        importacaoFisico.Layout.Should().BeSameAs(LayoutArquivoImportacao.C6CartaoCsv);
        importacaoFisico.Periodo.Should().Be((PeriodoArquivo)(FaturaCartaoC6.PrimeiraCompraFisico, FaturaCartaoC6.UltimaCompraFisico));
        importacaoFisico.Lidas.Should().Be(FaturaCartaoC6.LancamentosPorCartao);

        List<Transacao> transacoes = await LerTransacoesAsync();
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
        Guid idUsuario = await CadastrarUsuarioAsync(cadastro);

        await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.Completo());

        List<Transacao> transacoes = await LerTransacoesAsync();
        Transacao parcelada = transacoes.Should().ContainSingle(t => t.Parcela != null).Subject;
        parcelada.Parcela.Should().Be((Parcela)(3, 10));
        parcelada.Data.Should().Be(FaturaCartaoC6.PrimeiraCompraFisico);
        parcelada.CategoriaBanco.Should().Be("Eletrônicos");
        transacoes.Should().ContainSingle(t => t.CategoriaBanco == null);
    }

    [Theory, ApiAutoData]
    public async Task Post_Deve_DeixarONomeDoTitularForaDoQueEGravado(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await CadastrarUsuarioAsync(cadastro);

        await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.Completo());

        (await LerTransacoesAsync()).Should().AllSatisfy(t =>
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
        Guid idUsuario = await CadastrarUsuarioAsync(cadastro);
        AdicionarCartaoRequest fisico = cartao with { Banco = Banco.C6, Sufixo = FaturaCartaoC6.SufixoFisico, DiaVencimento = 15 };
        Guid idCartao = await AdicionarCartaoAsync(idUsuario, fisico);

        ImportarFaturaResponse corpo = await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.Completo(), diaVencimento: 10);

        ImportarFaturaResponse.CartaoImportado linhaFisico = corpo.Cartoes[0];
        linhaFisico.IdCartao.Should().Be(idCartao);
        linhaFisico.CartaoNovo.Should().BeFalse();
        linhaFisico.VencimentoPadrao.Should().BeFalse();
        corpo.Cartoes[1].CartaoNovo.Should().BeTrue();

        List<Cartao> cartoes = await LerCartoesAsync();
        cartoes.Should().HaveCount(2);
        cartoes.Single(c => c.Id == IdCartao.From(idCartao)).DiaVencimento.Should().Be((DiaVencimentoCartao)15);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_DiaUnicoEDiaDeUmFinal_Deve_AplicarOsDiasAosCartoesNovos(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await CadastrarUsuarioAsync(cadastro);

        ImportarFaturaResponse corpo = await ImportarComSucessoAsync(
            idUsuario,
            FaturaCartaoC6.Completo(),
            diaVencimento: 10,
            vencimentos: $$"""{"{{FaturaCartaoC6.SufixoVirtual}}":20,"9999":5}""");

        corpo.Cartoes.Should().AllSatisfy(c => c.VencimentoPadrao.Should().BeFalse());
        List<Cartao> cartoes = await LerCartoesAsync();
        cartoes.Should().HaveCount(2);
        cartoes.Single(c => c.Sufixo == FaturaCartaoC6.SufixoFisico).DiaVencimento.Should().Be((DiaVencimentoCartao)10);
        cartoes.Single(c => c.Sufixo == FaturaCartaoC6.SufixoVirtual).DiaVencimento.Should().Be((DiaVencimentoCartao)20);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_ReimportaAMesmaFatura_Deve_ManterAsTransacoesERegistrarAsNovasImportacoes(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await CadastrarUsuarioAsync(cadastro);
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
        (await LerTransacoesAsync()).Should().HaveCount(FaturaCartaoC6.QuantidadeLancamentos);
        (await LerCartoesAsync()).Should().HaveCount(2);
        (await LerImportacoesAsync()).Should().HaveCount(4);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_ParcelaSeguinteNaFaturaSeguinte_Deve_GravarUmaTransacaoParaCadaParcela(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await CadastrarUsuarioAsync(cadastro);
        await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.Completo());

        ImportarFaturaResponse corpo = await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.ComParcelaSeguinte());

        corpo.Cartoes[0].Novas.Should().Be(1);
        corpo.Cartoes[1].Novas.Should().Be(0);
        List<Transacao> transacoes = await LerTransacoesAsync();
        transacoes.Should().HaveCount(FaturaCartaoC6.QuantidadeLancamentos + 1);
        transacoes.Where(t => t.Parcela != null).Select(t => t.Parcela).Should().BeEquivalentTo([(Parcela)(3, 10), (Parcela)(4, 10)]);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_ArquivoDoExtrato_Deve_RetornarErroNoArquivoSemGravar(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, ExtratoContaC6.Completo());

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await CamposComErroAsync(resposta)).Should().Equal("arquivo");
        await DeveEstarSemImportacaoAsync();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_UsuarioNaoExiste_Deve_RetornarNaoEncontradoSemGravar(Guid idUsuario)
    {
        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, FaturaCartaoC6.Completo());

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await DeveEstarSemImportacaoAsync();
    }

    // A leitura do formulário para no limite da rota, antes do validator: o erro é geral, e não no campo do arquivo.
    [Theory, ApiAutoData]
    public async Task Post_Quando_FormularioAcimaDoLimiteDaRota_Deve_RecusarSemLerOArquivoInteiro(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, new byte[ImportarFaturaEndpoint.TamanhoMaximoFormulario + 1]);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CamposComErroAsync(resposta)).Should().Equal("generalErrors");
        await DeveEstarSemImportacaoAsync();
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

        using HttpResponseMessage resposta = await _api.Client.PostAsync(Rota(Guid.Empty), formulario);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CamposComErroAsync(resposta)).Should().BeEquivalentTo("idUsuario", "layout", "arquivo", "diaVencimento", "vencimentos");
        await DeveEstarSemImportacaoAsync();
    }

    private async Task<Guid> CadastrarUsuarioAsync(CadastrarUsuarioRequest cadastro)
    {
        using HttpResponseMessage resposta = await _api.Client.PostAsJsonAsync(_rotaCadastro, cadastro);
        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        CadastrarUsuarioResponse? corpo = await resposta.Content.ReadFromJsonAsync<CadastrarUsuarioResponse>();
        return corpo!.Id;
    }

    private async Task<Guid> AdicionarCartaoAsync(Guid idUsuario, AdicionarCartaoRequest cartao)
    {
        using HttpResponseMessage resposta = await _api.Client.PostAsJsonAsync(new Uri($"/api/usuarios/{idUsuario}/cartoes", UriKind.Relative), cartao);
        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        AdicionarCartaoResponse? corpo = await resposta.Content.ReadFromJsonAsync<AdicionarCartaoResponse>();
        return corpo!.Id;
    }

    private async Task<HttpResponseMessage> ImportarAsync(Guid idUsuario, byte[] arquivo, int? diaVencimento = null, string? vencimentos = null)
    {
        using ByteArrayContent conteudo = new(arquivo);
        conteudo.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

        using MultipartFormDataContent formulario = new()
        {
            { new StringContent(LayoutArquivoImportacao.C6CartaoCsv.Value), "layout" },
            { conteudo, "arquivo", "fatura.csv" },
        };

        if (diaVencimento is int dia)
            formulario.Add(new StringContent(dia.ToString(CultureInfo.InvariantCulture)), "diaVencimento");

        if (vencimentos is not null)
            formulario.Add(new StringContent(vencimentos), "vencimentos");

        return await _api.Client.PostAsync(Rota(idUsuario), formulario);
    }

    private async Task<ImportarFaturaResponse> ImportarComSucessoAsync(
        Guid idUsuario,
        byte[] arquivo,
        int? diaVencimento = null,
        string? vencimentos = null)
    {
        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, arquivo, diaVencimento, vencimentos);
        resposta.StatusCode.Should().Be(HttpStatusCode.OK, await resposta.Content.ReadAsStringAsync());
        return (await resposta.Content.ReadFromJsonAsync<ImportarFaturaResponse>())!;
    }

    private static Uri Rota(Guid idUsuario) => new($"/api/usuarios/{idUsuario}/importacoes/faturas", UriKind.Relative);

    private static async Task<IReadOnlyList<string>> CamposComErroAsync(HttpResponseMessage resposta)
    {
        using JsonDocument problema = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return problema.RootElement
            .GetProperty("errors")
            .EnumerateArray()
            .Select(erro => erro.GetProperty("name").GetString()!)
            .Distinct()
            .ToList();
    }

    private async Task DeveEstarSemImportacaoAsync()
    {
        (await _api.ConsultarAsync(db => db.Cartoes.CountAsync())).Should().Be(0);
        (await _api.ConsultarAsync(db => db.Transacoes.CountAsync())).Should().Be(0);
        (await _api.ConsultarAsync(db => db.Importacoes.CountAsync())).Should().Be(0);
    }

    private Task<List<Cartao>> LerCartoesAsync() =>
        _api.ConsultarAsync(db => db.Cartoes.AsNoTracking().ToListAsync());

    private Task<List<Transacao>> LerTransacoesAsync() =>
        _api.ConsultarAsync(db => db.Transacoes.AsNoTracking().ToListAsync());

    private Task<List<ImportacaoArquivo>> LerImportacoesAsync() =>
        _api.ConsultarAsync(db => db.Importacoes.AsNoTracking().ToListAsync());
}
