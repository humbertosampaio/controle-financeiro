using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.WebApi.Endpoints.Importacoes.PreVisualizarExtrato;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Importacoes.ImportarExtrato;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ControleFinanceiro.WebApi.Tests.Importacoes.PreVisualizarExtrato;

/// <summary>
/// A prévia usa o formulário e o validator da importação do extrato: as regras do validator ficam no
/// <c>ImportarExtratoValidatorTests</c>.
/// </summary>
[Collection(ApiCollection.Nome)]
public sealed class PreVisualizarExtratoEndpointTests : IAsyncLifetime
{
    private readonly ApiComPostgresFixture _api;

    public PreVisualizarExtratoEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory, ApiAutoData]
    public async Task Post_Quando_ExtratoDoC6_Deve_TrazerAsContrapartesDasSaidasSemGravar(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await PreVisualizarAsync(idUsuario, LayoutArquivoImportacao.C6ContaCsv, ExtratoContaC6.Completo());

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        PreVisualizarExtratoResponse corpo = (await resposta.Content.ReadFromJsonAsync<PreVisualizarExtratoResponse>())!;
        corpo.Beneficiarios.Should().Equal("LOJA FICTICIA, LTDA");
        await DeveEstarSemGravacaoAsync();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_ExtratoDoMercadoPago_Deve_TrazerAsContrapartesDasSaidasNaOrdemDoArquivo(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await PreVisualizarAsync(
            idUsuario,
            LayoutArquivoImportacao.MercadoPagoContaCsv,
            ExtratoContaMercadoPago.Completo(),
            ExtratoContaMercadoPago.SufixoConta);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        PreVisualizarExtratoResponse corpo = (await resposta.Content.ReadFromJsonAsync<PreVisualizarExtratoResponse>())!;
        corpo.Beneficiarios.Should().Equal("FULANO FICTICIO", "LOJA FICTICIA LTDA", "CONDOMINIO FICTICIO", "CICRANO FICTICIO", "Clube Ficticio");
        await DeveEstarSemGravacaoAsync();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_SaldoDoArquivoNaoFecha_Deve_RetornarErroNoArquivo(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await PreVisualizarAsync(idUsuario, LayoutArquivoImportacao.C6ContaCsv, ExtratoContaC6.ComSaldoQueNaoFecha());

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await resposta.CamposComErroAsync()).Should().Equal("arquivo");
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_UsuarioNaoExiste_Deve_RetornarNaoEncontrado(Guid idUsuario)
    {
        using HttpResponseMessage resposta = await PreVisualizarAsync(idUsuario, LayoutArquivoImportacao.C6ContaCsv, ExtratoContaC6.Completo());

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_Quando_RequisicaoInvalida_Deve_RetornarErroEmCadaCampo()
    {
        // O formulário multipart vazio não chega ao validator. O sufixo só é conferido nos layouts que o pedem.
        using MultipartFormDataContent formulario = new() { { new StringContent("12a4"), "sufixoConta" } };

        using HttpResponseMessage resposta = await _api.Client.PostAsync(Rota(Guid.Empty, layout: "layout-desconhecido"), formulario);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.CamposComErroAsync()).Should().BeEquivalentTo("idUsuario", "layout", "arquivo");
    }

    private async Task<HttpResponseMessage> PreVisualizarAsync(
        Guid idUsuario,
        LayoutArquivoImportacao layout,
        byte[] arquivo,
        string? sufixoConta = null)
    {
        using ByteArrayContent conteudo = new(arquivo);
        conteudo.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

        using MultipartFormDataContent formulario = new() { { conteudo, "arquivo", "extrato.csv" } };
        if (sufixoConta is not null)
            formulario.Add(new StringContent(sufixoConta), "sufixoConta");

        return await _api.Client.PostAsync(Rota(idUsuario, layout.Value), formulario);
    }

    private static Uri Rota(Guid idUsuario, string layout) => new($"/api/usuarios/{idUsuario}/importacoes/extratos/{layout}/previa", UriKind.Relative);

    private async Task DeveEstarSemGravacaoAsync()
    {
        (await _api.ObterTodosAsync(db => db.Contas)).Should().BeEmpty();
        (await _api.ObterTodosAsync(db => db.Transacoes)).Should().BeEmpty();
        (await _api.ObterTodosAsync(db => db.Importacoes)).Should().BeEmpty();
        (await _api.ObterTodosAsync(db => db.Beneficiarios)).Should().BeEmpty();
    }
}
