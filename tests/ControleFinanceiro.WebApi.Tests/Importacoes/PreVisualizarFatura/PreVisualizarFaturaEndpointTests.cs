using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.WebApi.Endpoints.Beneficiarios.CadastrarLote;
using ControleFinanceiro.WebApi.Endpoints.Importacoes.PreVisualizarFatura;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.AdicionarCartao;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Importacoes.ImportarFatura;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace ControleFinanceiro.WebApi.Tests.Importacoes.PreVisualizarFatura;

[Collection(ApiCollection.Nome)]
public sealed class PreVisualizarFaturaEndpointTests : IAsyncLifetime
{
    /// <summary>As descrições distintas das compras da fatura fictícia do C6, na ordem do arquivo.</summary>
    private static readonly string[] _descricoesC6 =
    [
        "MERCADO FICTICIO",
        "PADARIA EXEMPLO",
        "LOJA DE EXEMPLO",
        "SERVICO INTERNACIONAL FICTICIO",
        "APLICATIVO DE TRANSPORTE FICTICIO",
    ];

    private readonly ApiComPostgresFixture _api;

    public PreVisualizarFaturaEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory, ApiAutoData]
    public async Task Post_Quando_FaturaDoC6SemCartoesCadastrados_Deve_TrazerOsFinaisEAsDescricoesDasComprasSemGravar(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        PreVisualizarFaturaResponse corpo = await PreVisualizarComSucessoAsync(idUsuario, LayoutArquivoImportacao.C6CartaoCsv, FaturaCartaoC6.Completo());

        corpo.FinaisCartao.Should().Equal(
            new PreVisualizarFaturaResponse.FinalCartao(FaturaCartaoC6.SufixoFisico, Cadastrado: false, DiaVencimento: null),
            new PreVisualizarFaturaResponse.FinalCartao(FaturaCartaoC6.SufixoVirtual, Cadastrado: false, DiaVencimento: null));
        corpo.Beneficiarios.Should().Equal(_descricoesC6);
        await DeveEstarSemGravacaoAsync();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_TextoJaEAliasDoUsuario_Deve_OmitirODaPrevia(CadastrarUsuarioRequest cadastro, ItemBeneficiarioDto existente)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        Guid idExistente = (await _api.CadastrarBeneficiariosAsync(idUsuario, new() { Itens = [existente] }))[0];
        string[] resolvidas = [_descricoesC6[1], _descricoesC6[3]];

        // Uma vira alias do beneficiário existente e a outra de um novo, em outra caixa: a prévia compara o normalizado.
        await _api.CadastrarBeneficiariosAsync(idUsuario, new()
        {
            Itens =
            [
                new ItemBeneficiarioDto { IdBeneficiario = idExistente, Alias = resolvidas[0].ToLowerInvariant() },
                new ItemBeneficiarioDto { Nome = existente.Nome, Alias = resolvidas[1].ToLowerInvariant() },
            ],
        });

        PreVisualizarFaturaResponse corpo = await PreVisualizarComSucessoAsync(idUsuario, LayoutArquivoImportacao.C6CartaoCsv, FaturaCartaoC6.Completo());

        corpo.Beneficiarios.Should().Equal(_descricoesC6.Except(resolvidas));
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_FinalJaECartaoDoUsuario_Deve_MarcarOCartaoCadastradoComOVencimento(
        CadastrarUsuarioRequest cadastro,
        AdicionarCartaoRequest cartao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        AdicionarCartaoRequest fisico = cartao with { Banco = Banco.C6, Sufixo = FaturaCartaoC6.SufixoFisico };
        await _api.AdicionarCartaoAsync(idUsuario, fisico);

        PreVisualizarFaturaResponse corpo = await PreVisualizarComSucessoAsync(idUsuario, LayoutArquivoImportacao.C6CartaoCsv, FaturaCartaoC6.Completo());

        corpo.FinaisCartao.Should().Equal(
            new PreVisualizarFaturaResponse.FinalCartao(FaturaCartaoC6.SufixoFisico, Cadastrado: true, fisico.DiaVencimento),
            new PreVisualizarFaturaResponse.FinalCartao(FaturaCartaoC6.SufixoVirtual, Cadastrado: false, DiaVencimento: null));
        (await _api.ObterTodosAsync(db => db.Cartoes)).Should().ContainSingle();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_MesmoFinalEmCartaoDeOutroBanco_Deve_DeixarOFinalSemCadastro(
        CadastrarUsuarioRequest cadastro,
        AdicionarCartaoRequest cartao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await _api.AdicionarCartaoAsync(idUsuario, cartao with { Banco = Banco.MercadoPago, Sufixo = FaturaCartaoC6.SufixoFisico });

        PreVisualizarFaturaResponse corpo = await PreVisualizarComSucessoAsync(idUsuario, LayoutArquivoImportacao.C6CartaoCsv, FaturaCartaoC6.Completo());

        corpo.FinaisCartao.Should().AllSatisfy(f => f.Cadastrado.Should().BeFalse());
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_FaturaDoMercadoPago_Deve_TrazerOFinalEAsDescricoesDistintasDasCompras(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        PreVisualizarFaturaResponse corpo = await PreVisualizarComSucessoAsync(
            idUsuario,
            LayoutArquivoImportacao.MercadoPagoCartaoCsv,
            FaturaCartaoMercadoPago.Completo());

        corpo.FinaisCartao.Should().Equal(new PreVisualizarFaturaResponse.FinalCartao(FaturaCartaoMercadoPago.Sufixo, Cadastrado: false, DiaVencimento: null));
        corpo.Beneficiarios.Should().Equal("LOJA FICTICIA ELETRO", "PADARIA FICTICIA", "MERCADO FICTICIO", "LIVRARIA \"FICTICIA\"", "CAFE FICTICIO");
        await DeveEstarSemGravacaoAsync();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_ArquivoForaDoLayout_Deve_RetornarErroNoArquivo(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await PreVisualizarAsync(
            idUsuario,
            LayoutArquivoImportacao.C6CartaoCsv,
            Encoding.UTF8.GetBytes("isto não é uma fatura"));

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await resposta.CamposComErroAsync()).Should().Equal("arquivo");
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_UsuarioNaoExiste_Deve_RetornarNaoEncontrado(Guid idUsuario)
    {
        using HttpResponseMessage resposta = await PreVisualizarAsync(idUsuario, LayoutArquivoImportacao.C6CartaoCsv, FaturaCartaoC6.Completo());

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // A leitura do formulário para no limite da rota, antes do validator: o erro é geral, e não no campo do arquivo.
    [Theory, ApiAutoData]
    public async Task Post_Quando_FormularioAcimaDoLimiteDaRota_Deve_RecusarSemLerOArquivoInteiro(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await PreVisualizarAsync(
            idUsuario,
            LayoutArquivoImportacao.C6CartaoCsv,
            new byte[PreVisualizarFaturaEndpoint.TamanhoMaximoFormulario + 1]);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.CamposComErroAsync()).Should().Equal("generalErrors");
    }

    [Fact]
    public async Task Post_Quando_RequisicaoInvalida_Deve_RetornarErroEmCadaCampo()
    {
        // O formulário multipart vazio não chega ao validator; o campo a mais só o deixa não vazio.
        using MultipartFormDataContent formulario = new() { { new StringContent("ignorado"), "outro" } };

        using HttpResponseMessage resposta = await _api.Client.PostAsync(Rota(Guid.Empty, layout: "layout-desconhecido"), formulario);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.CamposComErroAsync()).Should().BeEquivalentTo("idUsuario", "layout", "arquivo");
    }

    private async Task<HttpResponseMessage> PreVisualizarAsync(Guid idUsuario, LayoutArquivoImportacao layout, byte[] arquivo)
    {
        using ByteArrayContent conteudo = new(arquivo);
        conteudo.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

        using MultipartFormDataContent formulario = new() { { conteudo, "arquivo", "fatura.csv" } };

        return await _api.Client.PostAsync(Rota(idUsuario, layout.Value), formulario);
    }

    private async Task<PreVisualizarFaturaResponse> PreVisualizarComSucessoAsync(Guid idUsuario, LayoutArquivoImportacao layout, byte[] arquivo)
    {
        using HttpResponseMessage resposta = await PreVisualizarAsync(idUsuario, layout, arquivo);
        resposta.StatusCode.Should().Be(HttpStatusCode.OK, await resposta.Content.ReadAsStringAsync());
        return (await resposta.Content.ReadFromJsonAsync<PreVisualizarFaturaResponse>())!;
    }

    private static Uri Rota(Guid idUsuario, string layout) => new($"/api/usuarios/{idUsuario}/importacoes/faturas/{layout}/previa", UriKind.Relative);

    private async Task DeveEstarSemGravacaoAsync()
    {
        (await _api.ObterTodosAsync(db => db.Cartoes)).Should().BeEmpty();
        (await _api.ObterTodosAsync(db => db.Transacoes)).Should().BeEmpty();
        (await _api.ObterTodosAsync(db => db.Importacoes)).Should().BeEmpty();
        (await _api.ObterTodosAsync(db => db.Beneficiarios)).Should().BeEmpty();
    }
}
