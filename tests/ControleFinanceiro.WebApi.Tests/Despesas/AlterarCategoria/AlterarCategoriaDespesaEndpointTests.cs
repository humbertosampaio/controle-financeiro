using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.WebApi.Endpoints.Beneficiarios.CadastrarLote;
using ControleFinanceiro.WebApi.Endpoints.Categorias.CadastrarLote;
using ControleFinanceiro.WebApi.Endpoints.Despesas.AlterarCategoria;
using ControleFinanceiro.WebApi.Endpoints.Despesas.CadastrarRecorrente;
using ControleFinanceiro.WebApi.Endpoints.Despesas.GerarDoMes;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Importacoes.ImportarFatura;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;
using System.Net.Http.Json;

namespace ControleFinanceiro.WebApi.Tests.Despesas.AlterarCategoria;

[Collection(ApiCollection.Nome)]
public sealed class AlterarCategoriaDespesaEndpointTests : IAsyncLifetime
{
    private readonly ApiComPostgresFixture _api;

    public AlterarCategoriaDespesaEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory, ApiAutoData]
    public async Task Put_Quando_DespesaGeradaPorRecorrente_Deve_TrocarACategoria(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteBeneficiariosRequest beneficiarios,
        CadastrarLoteCategoriasRequest categorias,
        CadastrarDespesaRecorrenteRequest recorrente,
        GerarDespesasDoMesRequest geracao,
        AlterarCategoriaDespesaRequest requisicao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        IReadOnlyList<Guid> idsBeneficiarios = await _api.CadastrarBeneficiariosAsync(idUsuario, beneficiarios);
        Guid idOutros = (await _api.ObterTodosAsync(db => db.Categorias)).Single().Id.Value;
        await _api.CadastrarDespesaRecorrenteAsync(
            idUsuario,
            recorrente with { IdBeneficiario = idsBeneficiarios[0], IdCategoria = idOutros, FormaPagamento = FormaPagamento.Pix.Value, IdCartao = null });
        using HttpResponseMessage gerada = await _api.Client.PostAsJsonAsync(
            new Uri($"/api/usuarios/{idUsuario}/despesas/geracoes", UriKind.Relative),
            geracao);
        gerada.StatusCode.Should().Be(HttpStatusCode.OK);
        Despesa despesa = (await _api.ObterTodosAsync(db => db.Despesas)).Single();
        Guid idCategoria = (await _api.CadastrarCategoriasAsync(idUsuario, categorias))[0];

        using HttpResponseMessage resposta = await AlterarAsync(idUsuario, despesa.Id.Value, requisicao with { IdCategoria = idCategoria });

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _api.ObterTodosAsync(db => db.Despesas)).Single().IdCategoria.Should().Be(IdCategoriaDespesa.From(idCategoria));
    }

    [Theory, ApiAutoData]
    public async Task Put_Quando_DespesaParcelada_Deve_TrocarACategoriaSemMudarAsParcelas(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteCategoriasRequest categorias,
        AlterarCategoriaDespesaRequest requisicao)
    {
        Guid idUsuario = await ImportarFaturaComParceladaAsync(cadastro);
        DespesaParcelada original = (await ObterParceladasAsync()).Single();
        Guid idCategoria = (await _api.CadastrarCategoriasAsync(idUsuario, categorias))[0];

        using HttpResponseMessage resposta = await AlterarAsync(idUsuario, original.Id.Value, requisicao with { IdCategoria = idCategoria });

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        DespesaParcelada gravada = (await ObterParceladasAsync()).Single();
        gravada.IdCategoria.Should().Be(IdCategoriaDespesa.From(idCategoria));
        gravada.Parcelas.Should().BeEquivalentTo(original.Parcelas);
    }

    [Theory, ApiAutoData]
    public async Task Put_Quando_DespesaNaoExiste_Deve_RetornarNaoEncontrado(
        CadastrarUsuarioRequest cadastro,
        CadastrarLoteCategoriasRequest categorias,
        AlterarCategoriaDespesaRequest requisicao,
        Guid idInexistente)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        Guid idCategoria = (await _api.CadastrarCategoriasAsync(idUsuario, categorias))[0];

        using HttpResponseMessage resposta = await AlterarAsync(idUsuario, idInexistente, requisicao with { IdCategoria = idCategoria });

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory, ApiAutoData]
    public async Task Put_Quando_DespesaDeOutroUsuario_Deve_RetornarNaoEncontradoSemAlterar(
        CadastrarUsuarioRequest cadastro,
        CadastrarUsuarioRequest outroCadastro,
        CadastrarLoteCategoriasRequest categorias,
        AlterarCategoriaDespesaRequest requisicao)
    {
        await ImportarFaturaComParceladaAsync(outroCadastro);
        DespesaParcelada doOutro = (await ObterParceladasAsync()).Single();
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        Guid idCategoria = (await _api.CadastrarCategoriasAsync(idUsuario, categorias))[0];

        using HttpResponseMessage resposta = await AlterarAsync(idUsuario, doOutro.Id.Value, requisicao with { IdCategoria = idCategoria });

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ObterParceladasAsync()).Single().IdCategoria.Should().Be(doOutro.IdCategoria);
    }

    [Theory, ApiAutoData]
    public async Task Put_Quando_CategoriaNaoExiste_Deve_RetornarNaoEncontradoSemAlterar(
        CadastrarUsuarioRequest cadastro,
        AlterarCategoriaDespesaRequest requisicao)
    {
        Guid idUsuario = await ImportarFaturaComParceladaAsync(cadastro);
        DespesaParcelada original = (await ObterParceladasAsync()).Single();

        using HttpResponseMessage resposta = await AlterarAsync(idUsuario, original.Id.Value, requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ObterParceladasAsync()).Single().IdCategoria.Should().Be(original.IdCategoria);
    }

    [Theory, ApiAutoData]
    public async Task Put_Quando_CategoriaDeOutroUsuario_Deve_RetornarNaoEncontradoSemAlterar(
        CadastrarUsuarioRequest cadastro,
        CadastrarUsuarioRequest outroCadastro,
        CadastrarLoteCategoriasRequest categoriasDoOutro,
        AlterarCategoriaDespesaRequest requisicao)
    {
        Guid idOutroUsuario = await _api.CadastrarUsuarioAsync(outroCadastro);
        Guid idCategoriaDoOutro = (await _api.CadastrarCategoriasAsync(idOutroUsuario, categoriasDoOutro))[0];
        Guid idUsuario = await ImportarFaturaComParceladaAsync(cadastro);
        DespesaParcelada original = (await ObterParceladasAsync()).Single();

        using HttpResponseMessage resposta = await AlterarAsync(idUsuario, original.Id.Value, requisicao with { IdCategoria = idCategoriaDoOutro });

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ObterParceladasAsync()).Single().IdCategoria.Should().Be(original.IdCategoria);
    }

    [Fact]
    public async Task Put_Quando_RequisicaoInvalida_Deve_RetornarErroEmCadaCampo()
    {
        using HttpResponseMessage resposta = await AlterarAsync(Guid.Empty, Guid.Empty, new AlterarCategoriaDespesaRequest { IdCategoria = Guid.Empty });

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.CamposComErroAsync()).Should().BeEquivalentTo("idUsuario", "idDespesa", "idCategoria");
    }

    private async Task<Guid> ImportarFaturaComParceladaAsync(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await _api.ImportarFaturaC6Async(idUsuario);

        return idUsuario;
    }

    private Task<List<DespesaParcelada>> ObterParceladasAsync() => _api.ObterTodosAsync(db => db.Despesas.OfType<DespesaParcelada>());

    private Task<HttpResponseMessage> AlterarAsync(Guid idUsuario, Guid idDespesa, AlterarCategoriaDespesaRequest requisicao)
    {
        return _api.Client.PutAsJsonAsync(
            new Uri($"/api/usuarios/{idUsuario}/despesas/{idDespesa}/categoria", UriKind.Relative),
            requisicao);
    }
}
