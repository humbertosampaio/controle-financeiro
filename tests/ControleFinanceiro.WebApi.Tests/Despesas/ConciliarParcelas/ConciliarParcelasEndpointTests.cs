using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.WebApi.Endpoints.Despesas.ConciliarParcelas;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Despesas.VincularTransacao;
using ControleFinanceiro.WebApi.Tests.Importacoes;
using ControleFinanceiro.WebApi.Tests.Importacoes.ImportarFatura;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;

namespace ControleFinanceiro.WebApi.Tests.Despesas.ConciliarParcelas;

[Collection(ApiCollection.Nome)]
public sealed class ConciliarParcelasEndpointTests : IAsyncLifetime
{
    private readonly ApiComPostgresFixture _api;

    public ConciliarParcelasEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory, ApiAutoData]
    public async Task Post_Quando_ParcelaImportada_Deve_VincularSoAParcelaDoMesmoNumero(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await ImportarFaturaAsync(cadastro);
        Transacao transacao = await _api.ObterTransacaoAsync(idUsuario, VinculoExtensions.CompraParcelada);

        ConciliarParcelasResponse corpo = await _api.ConciliarComSucessoAsync(idUsuario);

        corpo.Should().Be(new ConciliarParcelasResponse(Vinculadas: 1, SemParcela: 0));
        DespesaParcelada gravada = (await _api.ObterParceladasAsync()).Single();
        ParcelaDespesa paga = gravada.Parcelas.Single(p => p.TemCobranca);
        paga.NumeroParcela.Should().Be(3);
        paga.Pagamento.IdTransacao.Should().Be(transacao.Id);
        paga.Pagamento.ValorEfetivo.Should().Be(transacao.ValorAbsoluto);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_FaturaSeguinte_Deve_VincularAsDuasParcelasNaMesmaDespesa(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await ImportarFaturaAsync(cadastro);
        await _api.ConciliarComSucessoAsync(idUsuario);
        await _api.ImportarFaturaComSucessoAsync(idUsuario, LayoutArquivoImportacao.C6CartaoCsv, FaturaCartaoC6.ComParcelaSeguinte());

        ConciliarParcelasResponse corpo = await _api.ConciliarComSucessoAsync(idUsuario);

        corpo.Should().Be(new ConciliarParcelasResponse(Vinculadas: 1, SemParcela: 0));
        (await _api.ObterParceladasAsync())
            .Single()
            .Parcelas
            .Where(p => p.TemCobranca)
            .Select(p => p.NumeroParcela)
            .Should().BeEquivalentTo([3, 4]);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_ComprasGemeas_Deve_UsarUmaTransacaoEmCadaDespesa(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        LayoutArquivoImportacao layout = LayoutArquivoImportacao.C6CartaoCsv;
        byte[] arquivo = FaturaCartaoC6.ComCompraParceladaGemea();
        await _api.ResolverCartoesDaFaturaAsync(idUsuario, layout, arquivo);
        await _api.ResolverBeneficiariosDaFaturaAsync(idUsuario, layout, arquivo);
        await _api.ResolverCategoriasDaFaturaAsync(idUsuario, layout, arquivo);
        await _api.ImportarFaturaComSucessoAsync(idUsuario, layout, arquivo);

        ConciliarParcelasResponse corpo = await _api.ConciliarComSucessoAsync(idUsuario);

        corpo.Should().Be(new ConciliarParcelasResponse(Vinculadas: 2, SemParcela: 0));
        List<DespesaParcelada> gemeas = await _api.ObterParceladasAsync();
        gemeas.Should().HaveCount(2);
        gemeas.SelectMany(d => d.TransacoesCobranca).Should().OnlyHaveUniqueItems().And.HaveCount(2);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_ChamadaDeNovo_Deve_NaoVincularNemMudarOBanco(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await ImportarFaturaAsync(cadastro);
        await _api.ConciliarComSucessoAsync(idUsuario);
        Pagamento[] pagamentos = (await _api.ObterParceladasAsync())
            .Single()
            .Parcelas
            .OrderBy(p => p.NumeroParcela)
            .Select(p => p.Pagamento)
            .ToArray();

        ConciliarParcelasResponse corpo = await _api.ConciliarComSucessoAsync(idUsuario);

        corpo.Should().Be(new ConciliarParcelasResponse(Vinculadas: 0, SemParcela: 0));
        (await _api.ObterParceladasAsync())
            .Single()
            .Parcelas
            .OrderBy(p => p.NumeroParcela)
            .Select(p => p.Pagamento)
            .Should().Equal(pagamentos);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_VinculoManualExistente_Deve_ManterOVinculo(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await ImportarFaturaAsync(cadastro);
        DespesaParcelada despesa = (await _api.ObterParceladasAsync()).Single();
        Transacao transacao = await _api.ObterTransacaoAsync(idUsuario, VinculoExtensions.CompraParcelada);
        await _api.VincularComSucessoAsync(idUsuario, despesa.Id.Value, transacao.Id.Value);

        ConciliarParcelasResponse corpo = await _api.ConciliarComSucessoAsync(idUsuario);

        corpo.Should().Be(new ConciliarParcelasResponse(Vinculadas: 0, SemParcela: 0));
        (await _api.ObterParceladasAsync()).Single().TransacoesCobranca.Should().Equal(transacao.Id);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_DoisUsuarios_Deve_ConciliarSoAsTransacoesDoUsuario(CadastrarUsuarioRequest cadastro, CadastrarUsuarioRequest outroCadastro)
    {
        Guid idUsuario = await ImportarFaturaAsync(cadastro);
        Guid idOutro = await ImportarFaturaAsync(outroCadastro);

        ConciliarParcelasResponse corpo = await _api.ConciliarComSucessoAsync(idUsuario);

        corpo.Vinculadas.Should().Be(1);
        List<DespesaParcelada> parceladas = await _api.ObterParceladasAsync();
        parceladas.Single(d => d.IdUsuario == IdUsuario.From(idUsuario)).TransacoesCobranca.Should().ContainSingle();
        parceladas.Single(d => d.IdUsuario == IdUsuario.From(idOutro)).TransacoesCobranca.Should().BeEmpty();
    }

    [Fact]
    public async Task Post_Quando_IdUsuarioVazio_Deve_RetornarErroNoIdUsuario()
    {
        using HttpResponseMessage resposta = await _api.ConciliarAsync(Guid.Empty);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.CamposComErroAsync()).Should().Equal("idUsuario");
    }

    private async Task<Guid> ImportarFaturaAsync(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await _api.ImportarFaturaC6Async(idUsuario);

        return idUsuario;
    }
}
