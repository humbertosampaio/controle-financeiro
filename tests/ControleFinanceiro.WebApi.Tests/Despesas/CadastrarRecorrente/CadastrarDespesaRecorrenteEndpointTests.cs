using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.WebApi.Endpoints.Beneficiarios.CadastrarLote;
using ControleFinanceiro.WebApi.Endpoints.Despesas.CadastrarRecorrente;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.AdicionarCartao;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;
using System.Net.Http.Json;

namespace ControleFinanceiro.WebApi.Tests.Despesas.CadastrarRecorrente;

[Collection(ApiCollection.Nome)]
public sealed class CadastrarDespesaRecorrenteEndpointTests : IAsyncLifetime
{
    private readonly ApiComPostgresFixture _api;

    public CadastrarDespesaRecorrenteEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory, ApiAutoData]
    public async Task Post_Quando_NoCreditoComItensDoUsuario_Deve_GravarARecorrente(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        AdicionarCartaoRequest cartao,
        CadastrarDespesaRecorrenteRequest requisicao)
    {
        ItensDoUsuario itens = await CadastrarUsuarioComItensAsync(cadastro, beneficiarios, cartao);
        CadastrarDespesaRecorrenteRequest noCredito = itens.Aplicar(requisicao) with
        {
            FormaPagamento = FormaPagamento.Credito.Value,
            IdCartao = itens.IdCartao,
        };

        using HttpResponseMessage resposta = await CadastrarAsync(itens.IdUsuario, noCredito);

        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        CadastrarDespesaRecorrenteResponse? corpo = await resposta.Content.ReadFromJsonAsync<CadastrarDespesaRecorrenteResponse>();
        DespesaRecorrente gravada = (await _api.ObterTodosAsync(db => db.DespesasRecorrentes)).Should().ContainSingle().Subject;
        gravada.Id.Should().Be(IdDespesaRecorrente.From(corpo!.Id));
        gravada.IdUsuario.Should().Be(IdUsuario.From(itens.IdUsuario));
        gravada.IdBeneficiario.Should().Be(IdBeneficiario.From(itens.IdBeneficiario));
        gravada.Descricao.Should().Be((TextoObrigatorio)noCredito.Descricao);
        gravada.IdCategoria.Should().Be(IdCategoriaDespesa.From(itens.IdCategoria));
        gravada.FormaPagamento.Should().Be(FormaPagamento.Credito);
        gravada.IdCartao.Should().Be(IdCartao.From(itens.IdCartao));
        gravada.Valor.Should().Be((ReaisPositivo)noCredito.Valor);
        gravada.DiaOcorrencia.Should().Be((DiaOcorrenciaRecorrente)noCredito.DiaOcorrencia);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_NoPixSemCartao_Deve_GravarSemCartao(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        AdicionarCartaoRequest cartao,
        CadastrarDespesaRecorrenteRequest requisicao)
    {
        ItensDoUsuario itens = await CadastrarUsuarioComItensAsync(cadastro, beneficiarios, cartao);
        CadastrarDespesaRecorrenteRequest noPix = itens.Aplicar(requisicao) with
        {
            FormaPagamento = FormaPagamento.Pix.Value,
            IdCartao = null,
        };

        using HttpResponseMessage resposta = await CadastrarAsync(itens.IdUsuario, noPix);

        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        DespesaRecorrente gravada = (await _api.ObterTodosAsync(db => db.DespesasRecorrentes)).Should().ContainSingle().Subject;
        gravada.FormaPagamento.Should().Be(FormaPagamento.Pix);
        gravada.IdCartao.Should().BeNull();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_MesmoBeneficiarioEDescricao_Deve_RetornarConflitoNaDescricao(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        AdicionarCartaoRequest cartao,
        CadastrarDespesaRecorrenteRequest requisicao)
    {
        ItensDoUsuario itens = await CadastrarUsuarioComItensAsync(cadastro, beneficiarios, cartao);
        CadastrarDespesaRecorrenteRequest primeira = itens.Aplicar(requisicao) with
        {
            Descricao = "Condomínio",
            FormaPagamento = FormaPagamento.Boleto.Value,
            IdCartao = null,
        };
        using HttpResponseMessage criada = await CadastrarAsync(itens.IdUsuario, primeira);
        criada.StatusCode.Should().Be(HttpStatusCode.Created);

        using HttpResponseMessage resposta = await CadastrarAsync(itens.IdUsuario, primeira with { Descricao = "  condominio " });

        resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await resposta.CamposComErroAsync()).Should().Equal("descricao");
        (await ContarRecorrentesAsync()).Should().Be(1);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_ItensDeOutroUsuario_Deve_RetornarErroEmCadaItem(
        CadastrarUsuarioRequest cadastro,
        CadastrarUsuarioRequest outroCadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        AdicionarCartaoRequest cartao,
        CadastrarDespesaRecorrenteRequest requisicao)
    {
        ItensDoUsuario doOutro = await CadastrarUsuarioComItensAsync(outroCadastro, beneficiarios, cartao);
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        CadastrarDespesaRecorrenteRequest comItensDoOutro = doOutro.Aplicar(requisicao) with
        {
            FormaPagamento = FormaPagamento.Credito.Value,
            IdCartao = doOutro.IdCartao,
        };

        using HttpResponseMessage resposta = await CadastrarAsync(idUsuario, comItensDoOutro);

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await resposta.CamposComErroAsync()).Should().BeEquivalentTo("idBeneficiario", "idCategoria", "idCartao");
        (await ContarRecorrentesAsync()).Should().Be(0);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_UsuarioNaoExiste_Deve_RetornarNaoEncontrado(
        Guid idUsuario,
        CadastrarDespesaRecorrenteRequest requisicao)
    {
        using HttpResponseMessage resposta = await CadastrarAsync(idUsuario, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ContarRecorrentesAsync()).Should().Be(0);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_RequisicaoInvalida_Deve_RetornarErroEmCadaCampo(CadastrarDespesaRecorrenteRequest requisicao)
    {
        CadastrarDespesaRecorrenteRequest invalida = requisicao with
        {
            IdBeneficiario = Guid.Empty,
            Descricao = " ",
            IdCategoria = Guid.Empty,
            FormaPagamento = FormaPagamento.Credito.Value,
            IdCartao = null,
            Valor = -0.01m,
            DiaOcorrencia = 29,
        };

        using HttpResponseMessage resposta = await CadastrarAsync(Guid.Empty, invalida);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.CamposComErroAsync()).Should().BeEquivalentTo(
            "idUsuario", "idBeneficiario", "descricao", "idCategoria", "idCartao", "valor", "diaOcorrencia");
        (await ContarRecorrentesAsync()).Should().Be(0);
    }

    /// <summary>
    /// Cadastra o usuário com um beneficiário e um cartão. A categoria é a Outros, que nasce com o usuário.
    /// </summary>
    private async Task<ItensDoUsuario> CadastrarUsuarioComItensAsync(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        AdicionarCartaoRequest cartao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        IReadOnlyList<Guid> idsBeneficiarios = await _api.CadastrarBeneficiariosAsync(idUsuario, beneficiarios);
        Guid idCartao = await _api.AdicionarCartaoAsync(idUsuario, cartao);
        CategoriaDespesa outros = (await _api.ObterTodosAsync(db => db.Categorias))
            .Single(c => c.IdUsuario == IdUsuario.From(idUsuario));

        return new ItensDoUsuario(idUsuario, idsBeneficiarios[0], outros.Id, idCartao);
    }

    private Task<HttpResponseMessage> CadastrarAsync(Guid idUsuario, CadastrarDespesaRecorrenteRequest requisicao)
    {
        return _api.Client.PostAsJsonAsync(
            new Uri($"/api/usuarios/{idUsuario}/despesas-recorrentes", UriKind.Relative),
            requisicao);
    }

    private async Task<int> ContarRecorrentesAsync() => (await _api.ObterTodosAsync(db => db.DespesasRecorrentes)).Count;

    private sealed record class ItensDoUsuario(Guid IdUsuario, Guid IdBeneficiario, Guid IdCategoria, Guid IdCartao)
    {
        /// <summary>A requisição com o beneficiário e a categoria do usuário.</summary>
        public CadastrarDespesaRecorrenteRequest Aplicar(CadastrarDespesaRecorrenteRequest requisicao)
        {
            return requisicao with { IdBeneficiario = IdBeneficiario, IdCategoria = IdCategoria };
        }
    }
}
