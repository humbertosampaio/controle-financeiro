using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarExtrato;
using ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarExtratoMercadoPago;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ControleFinanceiro.WebApi.Tests.Importacoes.ImportarExtratoMercadoPago;

[Collection(ApiCollection.Nome)]
public sealed class ImportarExtratoMercadoPagoEndpointTests : IAsyncLifetime
{
    private readonly ApiComPostgresFixture _api;

    public ImportarExtratoMercadoPagoEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory, ApiAutoData]
    public async Task Post_Quando_PrimeiroExtratoDaConta_Deve_GravarAContaDePagamentoAsTransacoesEAImportacao(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, ExtratoContaMercadoPago.Completo());

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        ImportarExtratoResponse corpo = (await resposta.Content.ReadFromJsonAsync<ImportarExtratoResponse>())!;
        corpo.Lidas.Should().Be(ExtratoContaMercadoPago.QuantidadeLancamentos);
        corpo.Novas.Should().Be(ExtratoContaMercadoPago.QuantidadeLancamentos);
        corpo.JaExistentes.Should().Be(0);
        corpo.PeriodoNaoImportado.Should().BeNull();
        corpo.AvisoSomaPeriodo.Should().BeNull();

        ContaBancaria conta = (await _api.LerTodosAsync(db => db.Contas)).Should().ContainSingle().Subject;
        ((Guid)conta.Id).Should().Be(corpo.IdConta);
        conta.IdUsuario.Should().Be(IdUsuario.From(idUsuario));
        conta.Banco.Should().BeSameAs(Banco.MercadoPago);
        conta.Tipo.Should().BeSameAs(TipoConta.ContaPagamento);
        conta.Sufixo.Should().Be(ExtratoContaMercadoPago.SufixoConta);

        ImportacaoArquivo importacao = (await _api.LerTodosAsync(db => db.Importacoes)).Should().ContainSingle().Subject;
        ((Guid)importacao.Id).Should().Be(corpo.IdImportacao);
        importacao.Origem.Should().Be(OrigemTransacao.DaConta(conta.Id));
        importacao.Layout.Should().BeSameAs(LayoutArquivoImportacao.MercadoPagoContaCsv);
        importacao.Periodo.Should().Be(ExtratoContaMercadoPago.Periodo);

        List<Transacao> transacoes = await _api.LerTodosAsync(db => db.Transacoes);
        transacoes.Should().HaveCount(ExtratoContaMercadoPago.QuantidadeLancamentos);
        transacoes.Should().AllSatisfy(t =>
        {
            t.IdImportacao.Should().Be(importacao.Id);
            t.Origem.Should().Be(OrigemTransacao.DaConta(conta.Id));
        });
        transacoes.Sum(t => t.Valor).Should().Be(ExtratoContaMercadoPago.SomaLancamentos);
        transacoes.Select(t => t.Contraparte).Should().Contain("BELTRANO FICTICIO");
    }

    [Theory, ApiAutoData]
    public async Task Post_Deve_GravarAsTransacoesSemOCpfDaDescricao(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        await ImportarComSucessoAsync(idUsuario, ExtratoContaMercadoPago.Completo());

        (await _api.LerTodosAsync(db => db.Transacoes)).Should().AllSatisfy(t =>
        {
            string texto = $"{t.Descricao} {t.Contraparte}";
            ExtratoContaMercadoPago.CpfFicticio.Should().AllSatisfy(cpf => texto.Should().NotContain(cpf));
        });
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_ReimportaOMesmoArquivo_Deve_ManterAsTransacoesERegistrarANovaImportacao(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        ImportarExtratoResponse primeira = await ImportarComSucessoAsync(idUsuario, ExtratoContaMercadoPago.Completo());

        ImportarExtratoResponse segunda = await ImportarComSucessoAsync(idUsuario, ExtratoContaMercadoPago.Completo());

        segunda.Novas.Should().Be(0);
        segunda.JaExistentes.Should().Be(ExtratoContaMercadoPago.QuantidadeLancamentos);
        segunda.IdConta.Should().Be(primeira.IdConta);
        segunda.AvisoSomaPeriodo.Should().BeNull();
        (await _api.LerTodosAsync(db => db.Transacoes)).Should().HaveCount(ExtratoContaMercadoPago.QuantidadeLancamentos);
        (await _api.LerTodosAsync(db => db.Contas)).Should().ContainSingle();
        (await _api.LerTodosAsync(db => db.Importacoes)).Should().HaveCount(2);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_SaldoFinalNaoFecha_Deve_RetornarErroNoArquivoSemGravar(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, ExtratoContaMercadoPago.ComSaldoFinalQueNaoFecha());

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await resposta.CamposComErroAsync()).Should().Equal("arquivo");
        await DeveEstarSemImportacaoAsync();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_IdReferenciaRepetidoNoArquivo_Deve_RetornarErroNoArquivoSemGravar(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, ExtratoContaMercadoPago.ComIdReferenciaRepetido());

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await resposta.CamposComErroAsync()).Should().Equal("arquivo");
        await DeveEstarSemImportacaoAsync();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_UsuarioNaoExiste_Deve_RetornarNaoEncontradoSemGravar(Guid idUsuario)
    {
        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, ExtratoContaMercadoPago.Completo());

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await DeveEstarSemImportacaoAsync();
    }

    // A leitura do formulário para no limite da rota, antes do validator: o erro é geral, e não no campo do arquivo.
    [Theory, ApiAutoData]
    public async Task Post_Quando_FormularioAcimaDoLimiteDaRota_Deve_RecusarSemLerOArquivoInteiro(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, new byte[ImportarExtratoMercadoPagoEndpoint.TamanhoMaximoFormulario + 1]);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.CamposComErroAsync()).Should().Equal("generalErrors");
        await DeveEstarSemImportacaoAsync();
    }

    [Fact]
    public async Task Post_Quando_RequisicaoInvalida_Deve_RetornarErroEmCadaCampo()
    {
        using MultipartFormDataContent formulario = new() { { new StringContent("12a4"), "sufixoConta" } };

        using HttpResponseMessage resposta = await _api.Client.PostAsync(Rota(Guid.Empty), formulario);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.CamposComErroAsync()).Should().BeEquivalentTo("idUsuario", "arquivo", "sufixoConta");
        await DeveEstarSemImportacaoAsync();
    }

    private async Task<HttpResponseMessage> ImportarAsync(Guid idUsuario, byte[] arquivo)
    {
        using ByteArrayContent conteudo = new(arquivo);
        conteudo.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

        using MultipartFormDataContent formulario = new()
        {
            { conteudo, "arquivo", "extrato.csv" },
            { new StringContent(ExtratoContaMercadoPago.SufixoConta.Value), "sufixoConta" },
        };

        return await _api.Client.PostAsync(Rota(idUsuario), formulario);
    }

    private async Task<ImportarExtratoResponse> ImportarComSucessoAsync(Guid idUsuario, byte[] arquivo)
    {
        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, arquivo);
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await resposta.Content.ReadFromJsonAsync<ImportarExtratoResponse>())!;
    }

    private static Uri Rota(Guid idUsuario) => new($"/api/usuarios/{idUsuario}/importacoes/mercado-pago-conta-csv", UriKind.Relative);

    private async Task DeveEstarSemImportacaoAsync()
    {
        (await _api.LerTodosAsync(db => db.Contas)).Should().BeEmpty();
        (await _api.LerTodosAsync(db => db.Transacoes)).Should().BeEmpty();
        (await _api.LerTodosAsync(db => db.Importacoes)).Should().BeEmpty();
    }
}
