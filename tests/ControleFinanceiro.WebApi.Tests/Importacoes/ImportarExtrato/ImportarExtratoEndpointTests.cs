using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarExtrato;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ControleFinanceiro.WebApi.Tests.Importacoes.ImportarExtrato;

[Collection(ApiCollection.Nome)]
public sealed class ImportarExtratoEndpointTests : IAsyncLifetime
{
    private static readonly Uri _rotaCadastro = new("/api/usuarios", UriKind.Relative);

    private readonly ApiComPostgresFixture _api;

    public ImportarExtratoEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // A conta nova, as transações e a importação vão no mesmo SaveChanges. O EF não conhece as FKs da origem e não
    // garante inserir a conta antes; as FKs adiadas para o commit cobrem qualquer ordem.
    [Theory, ApiAutoData]
    public async Task Post_Quando_PrimeiroExtratoDaConta_Deve_GravarAContaAsTransacoesEAImportacaoJuntas(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, ExtratoContaC6.Completo());

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        ImportarExtratoResponse corpo = (await resposta.Content.ReadFromJsonAsync<ImportarExtratoResponse>())!;
        corpo.Lidas.Should().Be(ExtratoContaC6.QuantidadeLancamentos);
        corpo.Novas.Should().Be(ExtratoContaC6.QuantidadeLancamentos);
        corpo.JaExistentes.Should().Be(0);
        corpo.PeriodoNaoImportado.Should().BeNull();
        corpo.AvisoSomaPeriodo.Should().BeNull();

        ContaBancaria conta = (await LerContasAsync()).Should().ContainSingle().Subject;
        ((Guid)conta.Id).Should().Be(corpo.IdConta);
        conta.IdUsuario.Should().Be(IdUsuario.From(idUsuario));
        conta.Banco.Should().BeSameAs(Banco.C6);
        conta.Tipo.Should().BeSameAs(TipoConta.ContaCorrente);
        conta.Sufixo.Value.Should().Be(ExtratoContaC6.SufixoConta);

        ImportacaoArquivo importacao = (await LerImportacoesAsync()).Should().ContainSingle().Subject;
        ((Guid)importacao.Id).Should().Be(corpo.IdImportacao);
        importacao.Origem.Should().Be(OrigemTransacao.DaConta(conta.Id));
        importacao.Layout.Should().BeSameAs(LayoutArquivoImportacao.C6ContaCsv);

        List<Transacao> transacoes = await LerTransacoesAsync();
        transacoes.Should().HaveCount(ExtratoContaC6.QuantidadeLancamentos);
        transacoes.Should().AllSatisfy(t =>
        {
            t.IdUsuario.Should().Be(IdUsuario.From(idUsuario));
            t.IdImportacao.Should().Be(importacao.Id);
            t.Origem.Should().Be(OrigemTransacao.DaConta(conta.Id));
        });
        transacoes.Sum(t => t.Valor).Should().Be(1841.99m);
    }

    [Theory, ApiAutoData]
    public async Task Post_Deve_GravarSoOSufixoDaConta(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await CadastrarUsuarioAsync(cadastro);

        await ImportarComSucessoAsync(idUsuario, ExtratoContaC6.Completo());

        ContaBancaria conta = (await LerContasAsync()).Should().ContainSingle().Subject;
        conta.Sufixo.Value.Should().NotContain(ExtratoContaC6.NumeroCompletoConta);
        conta.Apelido.Value.Should().NotContain(ExtratoContaC6.NumeroCompletoConta);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_LancamentosIdenticosNoMesmoDia_Deve_GravarUmaTransacaoParaCada(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await CadastrarUsuarioAsync(cadastro);

        await ImportarComSucessoAsync(idUsuario, ExtratoContaC6.Completo());

        List<Transacao> tarifas = (await LerTransacoesAsync())
            .Where(t => t.Data == ExtratoContaC6.DiaDasTarifasIdenticas && t.Descricao.Value == "Tarifa de exemplo")
            .ToList();
        tarifas.Should().HaveCount(2);
        tarifas.Should().OnlyHaveUniqueItems(t => t.Chave);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_ReimportaOMesmoArquivo_Deve_ManterAsTransacoesERegistrarANovaImportacao(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await CadastrarUsuarioAsync(cadastro);
        ImportarExtratoResponse primeira = await ImportarComSucessoAsync(idUsuario, ExtratoContaC6.Completo());

        ImportarExtratoResponse segunda = await ImportarComSucessoAsync(idUsuario, ExtratoContaC6.Completo());

        segunda.Novas.Should().Be(0);
        segunda.JaExistentes.Should().Be(ExtratoContaC6.QuantidadeLancamentos);
        segunda.IdConta.Should().Be(primeira.IdConta);
        List<Transacao> transacoes = await LerTransacoesAsync();
        transacoes.Should().HaveCount(ExtratoContaC6.QuantidadeLancamentos);
        transacoes.Should().AllSatisfy(t => ((Guid)t.IdImportacao).Should().Be(primeira.IdImportacao));
        (await LerContasAsync()).Should().ContainSingle();
        (await LerImportacoesAsync()).Should().HaveCount(2);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_PeriodoSobrepostoAoJaImportado_Deve_GravarSoAsNovas(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await CadastrarUsuarioAsync(cadastro);
        ImportarExtratoResponse inicio = await ImportarComSucessoAsync(idUsuario, ExtratoContaC6.Recortado(ExtratoContaC6.Inicio, new DateOnly(2026, 3, 4)));

        ImportarExtratoResponse completo = await ImportarComSucessoAsync(idUsuario, ExtratoContaC6.Completo());

        inicio.Novas.Should().Be(7);
        completo.Novas.Should().Be(3);
        completo.JaExistentes.Should().Be(7);
        completo.PeriodoNaoImportado.Should().BeNull();
        completo.AvisoSomaPeriodo.Should().BeNull();
        List<Transacao> transacoes = await LerTransacoesAsync();
        transacoes.Should().HaveCount(ExtratoContaC6.QuantidadeLancamentos);
        transacoes.Count(t => t.IdImportacao == IdImportacao.From(completo.IdImportacao)).Should().Be(3);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_DiasSemImportacaoDesdeAAnterior_Deve_AvisarOPeriodoNaoImportado(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await CadastrarUsuarioAsync(cadastro);
        await ImportarComSucessoAsync(idUsuario, ExtratoContaC6.Recortado(ExtratoContaC6.Inicio, new DateOnly(2026, 3, 4)));

        ImportarExtratoResponse corpo = await ImportarComSucessoAsync(
            idUsuario,
            ExtratoContaC6.Recortado(new DateOnly(2026, 3, 7), new DateOnly(2026, 3, 9)));

        corpo.PeriodoNaoImportado.Should().Be(new ImportarExtratoResponse.Periodo(new DateOnly(2026, 3, 5), new DateOnly(2026, 3, 6)));
        corpo.Novas.Should().Be(3);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_GravadaNoPeriodoQueOArquivoNaoTraz_Deve_AvisarASomaEGravarAsNovas(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await CadastrarUsuarioAsync(cadastro);
        await ImportarComSucessoAsync(idUsuario, ExtratoContaC6.Completo());

        ImportarExtratoResponse corpo = await ImportarComSucessoAsync(idUsuario, ExtratoContaC6.ComDescricaoTrocada());

        corpo.AvisoSomaPeriodo.Should().Be(
            "A soma das transações gravadas de 02/03/2026 a 09/03/2026 não fecha com a soma dos lançamentos do extrato.");
        corpo.Novas.Should().Be(1);
        (await LerTransacoesAsync()).Should().HaveCount(ExtratoContaC6.QuantidadeLancamentos + 1);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_ArquivoForaDoLayout_Deve_RetornarErroNoArquivoSemGravar(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, Encoding.UTF8.GetBytes("isto não é um extrato"));

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await CamposComErroAsync(resposta)).Should().Equal("arquivo");
        await DeveEstarSemImportacaoAsync();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_SaldoDoArquivoNaoFecha_Deve_RetornarErroNoArquivoSemGravar(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, ExtratoContaC6.ComSaldoQueNaoFecha());

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await CamposComErroAsync(resposta)).Should().Equal("arquivo");
        await DeveEstarSemImportacaoAsync();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_UsuarioNaoExiste_Deve_RetornarNaoEncontradoSemGravar(Guid idUsuario)
    {
        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, ExtratoContaC6.Completo());

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await DeveEstarSemImportacaoAsync();
    }

    // A leitura do formulário para no limite da rota, antes do validator: o erro é geral, e não no campo do arquivo.
    [Theory, ApiAutoData]
    public async Task Post_Quando_FormularioAcimaDoLimiteDaRota_Deve_RecusarSemLerOArquivoInteiro(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, new byte[ImportarExtratoEndpoint.TamanhoMaximoFormulario + 1]);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CamposComErroAsync(resposta)).Should().Equal("generalErrors");
        await DeveEstarSemImportacaoAsync();
    }

    [Fact]
    public async Task Post_Quando_RequisicaoInvalida_Deve_RetornarErroEmCadaCampo()
    {
        using MultipartFormDataContent formulario = new() { { new StringContent("layout-desconhecido"), "layout" } };

        using HttpResponseMessage resposta = await _api.Client.PostAsync(Rota(Guid.Empty), formulario);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CamposComErroAsync(resposta)).Should().BeEquivalentTo("idUsuario", "layout", "arquivo");
        await DeveEstarSemImportacaoAsync();
    }

    private async Task<Guid> CadastrarUsuarioAsync(CadastrarUsuarioRequest cadastro)
    {
        using HttpResponseMessage resposta = await _api.Client.PostAsJsonAsync(_rotaCadastro, cadastro);
        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        CadastrarUsuarioResponse? corpo = await resposta.Content.ReadFromJsonAsync<CadastrarUsuarioResponse>();
        return corpo!.Id;
    }

    private async Task<HttpResponseMessage> ImportarAsync(Guid idUsuario, byte[] arquivo)
    {
        using ByteArrayContent conteudo = new(arquivo);
        conteudo.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

        using MultipartFormDataContent formulario = new()
        {
            { new StringContent(LayoutArquivoImportacao.C6ContaCsv.Value), "layout" },
            { conteudo, "arquivo", "extrato.csv" },
        };

        return await _api.Client.PostAsync(Rota(idUsuario), formulario);
    }

    private async Task<ImportarExtratoResponse> ImportarComSucessoAsync(Guid idUsuario, byte[] arquivo)
    {
        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, arquivo);
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await resposta.Content.ReadFromJsonAsync<ImportarExtratoResponse>())!;
    }

    private static Uri Rota(Guid idUsuario) => new($"/api/usuarios/{idUsuario}/importacoes", UriKind.Relative);

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
        (await _api.ConsultarAsync(db => db.Contas.CountAsync())).Should().Be(0);
        (await _api.ConsultarAsync(db => db.Transacoes.CountAsync())).Should().Be(0);
        (await _api.ConsultarAsync(db => db.Importacoes.CountAsync())).Should().Be(0);
    }

    private Task<List<ContaBancaria>> LerContasAsync() =>
        _api.ConsultarAsync(db => db.Contas.AsNoTracking().ToListAsync());

    private Task<List<Transacao>> LerTransacoesAsync() =>
        _api.ConsultarAsync(db => db.Transacoes.AsNoTracking().ToListAsync());

    private Task<List<ImportacaoArquivo>> LerImportacoesAsync() =>
        _api.ConsultarAsync(db => db.Importacoes.AsNoTracking().ToListAsync());
}
