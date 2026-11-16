using ControleFinanceiro.Application.IA;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Mcp.Tests.Infraestrutura;
using ControleFinanceiro.Tests.Shared.Respostas;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text.Json;

namespace ControleFinanceiro.Mcp.Tests.Ferramentas;

/// <summary>
/// As prévias da importação, uma por layout, pelo processo real, com as fixtures fictícias na pasta de origem temporária
/// do servidor: elas contam, mas nunca gravam, e nunca mandam ao modelo uma linha do arquivo.
/// </summary>
[Collection(ServidorMcpCollection.Nome)]
public sealed class PreverImportacaoToolTests : IAsyncLifetime
{
    private readonly ServidorMcpFixture _servidor;
    private readonly ImportacaoDeTeste _importacao;

    public PreverImportacaoToolTests(ServidorMcpFixture servidor)
    {
        _servidor = servidor;
        _importacao = new ImportacaoDeTeste(servidor);
    }

    public static readonly TheoryData<string, string> Arquivos = new()
    {
        { "c6-conta-csv", ImportacaoDeTeste.ExtratoC6 },
        { "mercado-pago-conta-csv", ImportacaoDeTeste.ExtratoMercadoPago },
        { "c6-cartao-csv", ImportacaoDeTeste.FaturaC6 },
        { "mercado-pago-cartao-csv", ImportacaoDeTeste.FaturaMercadoPago },
    };

    public Task InitializeAsync() => _servidor.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // Sem o tipo, o cliente manda o sufixo e o vencimento como número, e o SDK não os converte para texto.
    [Theory]
    [InlineData("prever_importacao_c6_conta_csv", new[] { "caminho" })]
    [InlineData("prever_importacao_mercado_pago_conta_csv", new[] { "caminho", "sufixoConta" })]
    [InlineData("prever_importacao_c6_cartao_csv", new[] { "caminho", "vencimento" })]
    [InlineData("prever_importacao_mercado_pago_cartao_csv", new[] { "caminho" })]
    public async Task Esquema_Deve_TerSoOsParametrosDoLayoutComoTextoObrigatorio(string nome, string[] parametros)
    {
        McpClientTool ferramenta = await _servidor.ClienteMcp.ObterFerramentaAsync(nome);

        JsonElement esquema = ferramenta.ProtocolTool.InputSchema;
        JsonProperty[] propriedades = esquema.GetProperty("properties").EnumerateObject().ToArray();
        propriedades.Select(propriedade => propriedade.Name).Should().Equal(parametros);
        propriedades.Select(propriedade => propriedade.Value.GetProperty("type").GetString()).Should().AllBe("string");
        esquema.GetProperty("required").EnumerateArray().Select(obrigatorio => obrigatorio.GetString()).Should().BeEquivalentTo(parametros);
    }

    [Theory, MemberData(nameof(Arquivos))]
    public async Task Prever_Quando_CadastrosResolvidos_Deve_ContarOsLancamentosSemGravar(string codigoLayout, string caminho)
    {
        LayoutArquivoImportacao layout = LayoutArquivoImportacao.FromValue(codigoLayout);
        await _importacao.ResolverAsync(layout, caminho);

        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(layout, caminho);

        IReadOnlyList<Lancamento> lancamentos = await ImportacaoDeTeste.LerLancamentosAsync(layout, caminho);
        ImportacaoLida importacao = new(resultado.Json);
        importacao.Lidas.Should().Be(lancamentos.Count);
        importacao.Novas.Should().Be(lancamentos.Count);
        (await _importacao.LerGravadoAsync()).Should().Be(Gravado.Nada);
    }

    [Theory, MemberData(nameof(Arquivos))]
    public async Task Prever_Deve_NaoMandarADescricaoNemAContraparteDoArquivo(string codigoLayout, string caminho)
    {
        LayoutArquivoImportacao layout = LayoutArquivoImportacao.FromValue(codigoLayout);
        await _importacao.ResolverAsync(layout, caminho);

        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(layout, caminho);

        IReadOnlyList<Lancamento> lancamentos = await ImportacaoDeTeste.LerLancamentosAsync(layout, caminho);
        resultado.Json.Should().NotContainKey("erro");
        IEnumerable<string> textos = lancamentos
            .Select(lancamento => (string)lancamento.Descricao)
            .Concat(lancamentos.Select(lancamento => lancamento.Contraparte).OfType<string>().Where(texto => texto.Length > 0));
        resultado.Texto.Should().NotContainAny(textos).And.NotContain(caminho);
    }

    [Fact]
    public async Task Prever_Quando_ExtratoDoC6_Deve_MandarSoOFinalDaContaDoCabecalho()
    {
        await _importacao.ResolverAsync(LayoutArquivoImportacao.C6ContaCsv, ImportacaoDeTeste.ExtratoC6);

        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(LayoutArquivoImportacao.C6ContaCsv, ImportacaoDeTeste.ExtratoC6);

        string numeroConta = ImportacaoDeTeste.NumeroContaDoCabecalhoC6();
        resultado.Texto.Should().NotContain(numeroConta).And.Contain($"conta final {numeroConta[^4..]}");
    }

    [Fact]
    public async Task Prever_Quando_ContaNaoCadastrada_Deve_DizerQueCriaAConta()
    {
        await _importacao.ResolverAsync(LayoutArquivoImportacao.C6ContaCsv, ImportacaoDeTeste.ExtratoC6);

        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(LayoutArquivoImportacao.C6ContaCsv, ImportacaoDeTeste.ExtratoC6);

        new ImportacaoLida(resultado.Json).ContaNova.Should().BeTrue();
    }

    [Fact]
    public async Task Prever_Quando_CaminhoAbsolutoNaOrigem_Deve_LerOArquivo()
    {
        await _importacao.ResolverAsync(LayoutArquivoImportacao.C6ContaCsv, ImportacaoDeTeste.ExtratoC6);

        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(
            LayoutArquivoImportacao.C6ContaCsv,
            ImportacaoDeTeste.Completo(ImportacaoDeTeste.ExtratoC6));

        resultado.Json.Should().NotContainKey("erro");
    }

    [Fact]
    public async Task Prever_Quando_FinalSemCartao_Deve_DevolverOsFinaisSemGravar()
    {
        await _importacao.ResolverAliasesAsync(LayoutArquivoImportacao.C6CartaoCsv, ImportacaoDeTeste.FaturaC6);

        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(LayoutArquivoImportacao.C6CartaoCsv, ImportacaoDeTeste.FaturaC6);

        Fatura fatura = await ImportacaoDeTeste.LerFaturaAsync(LayoutArquivoImportacao.C6CartaoCsv, ImportacaoDeTeste.FaturaC6);
        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Importacao.CartoesNaoResolvidos.Mensagem);
        PendenciasImportacaoLidas.DoEnvelope(resultado.Json).FinaisSemCartao.Should().Equal(fatura.Sufixos.Select(sufixo => (string)sufixo));
        (await _importacao.LerGravadoAsync()).Should().Be(Gravado.Nada);
    }

    // O esquema pede o parâmetro, e o SDK recusa a chamada sem ele antes de chegar à importação.
    [Fact]
    public async Task Prever_Quando_SemOSufixoObrigatorio_Deve_DevolverErroSemGravar()
    {
        await _importacao.ResolverAsync(LayoutArquivoImportacao.MercadoPagoContaCsv, ImportacaoDeTeste.ExtratoMercadoPago);

        CallToolResult resultado = await _servidor.ClienteMcp.CallToolAsync(
            Tools.PreverImportacaoMercadoPagoContaCsv,
            new Dictionary<string, object?> { ["caminho"] = ImportacaoDeTeste.ExtratoMercadoPago });

        resultado.IsError.Should().BeTrue();
        (await _importacao.LerGravadoAsync()).Should().Be(Gravado.Nada);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    public async Task Prever_Quando_ExtratoDoMercadoPagoComSufixoInvalido_Deve_DevolverSufixoContaInvalido(string sufixoConta)
    {
        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(
            LayoutArquivoImportacao.MercadoPagoContaCsv,
            ImportacaoDeTeste.ExtratoMercadoPago,
            sufixoConta);

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Importacao.SufixoContaInvalido.Mensagem);
    }

    [Theory]
    [InlineData("")]
    [InlineData("10/04/2026")]
    public async Task Prever_Quando_FaturaC6SemVencimentoValido_Deve_DevolverVencimentoObrigatorioSemGravar(string vencimento)
    {
        await _importacao.ResolverAsync(LayoutArquivoImportacao.C6CartaoCsv, ImportacaoDeTeste.FaturaC6);

        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(LayoutArquivoImportacao.C6CartaoCsv, ImportacaoDeTeste.FaturaC6, vencimento: vencimento);

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Importacao.VencimentoObrigatorio.Mensagem);
        (await _importacao.LerGravadoAsync()).Should().Be(Gravado.Nada);
    }

    [Fact]
    public async Task Prever_Quando_VencimentoAntesDasCompras_Deve_DevolverVencimentoIncompativelSemGravar()
    {
        await _importacao.ResolverAsync(LayoutArquivoImportacao.C6CartaoCsv, ImportacaoDeTeste.FaturaC6);

        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(LayoutArquivoImportacao.C6CartaoCsv, ImportacaoDeTeste.FaturaC6, vencimento: "2026-03-01");

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Importacao.VencimentoIncompativel.Mensagem);
        (await _importacao.LerGravadoAsync()).Should().Be(Gravado.Nada);
    }

    [Fact]
    public async Task Prever_Quando_FaturaJaImportadaComOutroVencimentoNoMesmoMes_Deve_DevolverFaturaJaImportadaSemGravar()
    {
        await _importacao.ResolverAsync(LayoutArquivoImportacao.C6CartaoCsv, ImportacaoDeTeste.FaturaC6);
        CallToolResult previa = await _servidor.ClienteMcp.PreverImportacaoAsync(LayoutArquivoImportacao.C6CartaoCsv, ImportacaoDeTeste.FaturaC6);
        await _servidor.ClienteMcp.ConfirmarImportacaoAsync(
            LayoutArquivoImportacao.C6CartaoCsv,
            new ImportacaoLida(previa.Json).Resumo,
            ImportacaoDeTeste.FaturaC6);
        Gravado gravadoAposPrimeira = await _importacao.LerGravadoAsync();

        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(LayoutArquivoImportacao.C6CartaoCsv, ImportacaoDeTeste.FaturaC6, vencimento: "2026-04-13");

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Importacao.FaturaJaImportadaComOutroVencimento.Mensagem);
        (await _importacao.LerGravadoAsync()).Should().Be(gravadoAposPrimeira);
    }

    [Fact]
    public async Task Prever_Quando_FaturaSemPagamento_Deve_TrazerOAviso()
    {
        string caminho = ImportacaoDeTeste.GravarComLinhaTrocada(
            ImportacaoDeTeste.FaturaC6,
            numero: 6,
            "02/03/2026;FULANA DE TAL FICTICIA;1111;-;CREDITO FICTICIO;Única;0;0;-500.00");
        await _importacao.ResolverAsync(LayoutArquivoImportacao.C6CartaoCsv, caminho);

        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(LayoutArquivoImportacao.C6CartaoCsv, caminho);

        new ImportacaoLida(resultado.Json).Avisos.Should().ContainSingle()
            .Which.Should().Be("A fatura não traz o pagamento da fatura anterior: se não é a primeira fatura do cartão, confira se o layout do banco mudou.");
    }

    [Fact]
    public async Task Prever_Quando_FaturaComPagamento_Deve_NaoTrazerAviso()
    {
        await _importacao.ResolverAsync(LayoutArquivoImportacao.C6CartaoCsv, ImportacaoDeTeste.FaturaC6);

        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(LayoutArquivoImportacao.C6CartaoCsv, ImportacaoDeTeste.FaturaC6);

        new ImportacaoLida(resultado.Json).Avisos.Should().BeEmpty();
    }

    [Fact]
    public async Task Prever_Quando_BeneficiarioSemAlias_Deve_DevolverAQuantidade()
    {
        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(LayoutArquivoImportacao.C6ContaCsv, ImportacaoDeTeste.ExtratoC6);

        IReadOnlyList<Lancamento> lancamentos = await ImportacaoDeTeste.LerLancamentosAsync(LayoutArquivoImportacao.C6ContaCsv, ImportacaoDeTeste.ExtratoC6);
        int pendentes = AliasesBeneficiarioPendentes.Listar(_servidor.Usuario.Id, beneficiariosDoUsuario: [], lancamentos).Count;
        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Importacao.BeneficiariosNaoResolvidos.Mensagem);
        PendenciasImportacaoLidas.DoEnvelope(resultado.Json).Beneficiarios.Should().Be(pendentes).And.BePositive();
    }

    [Fact]
    public async Task Prever_Quando_CaminhoSaiDaOrigem_Deve_DevolverArquivoForaDaOrigem()
    {
        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(LayoutArquivoImportacao.C6ContaCsv, "dados/../fora.csv");

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Importacao.ArquivoForaDaOrigem.Mensagem);
    }

    [Fact]
    public async Task Prever_Quando_ArquivoNaoExiste_Deve_DevolverArquivoNaoEncontrado()
    {
        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(LayoutArquivoImportacao.C6ContaCsv, "dados/inexistente.csv");

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Importacao.ArquivoNaoEncontrado.Mensagem);
    }

    [Fact]
    public async Task Prever_Quando_LinhaForaDoLayout_Deve_DevolverSoONumeroDaLinha()
    {
        string caminho = ImportacaoDeTeste.GravarComLinhaTrocada(ImportacaoDeTeste.ExtratoC6, numero: 13, "03/03/2026,03/03/2026,TEXTO-DO-ARQUIVO,,0.00,5.00");

        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(LayoutArquivoImportacao.C6ContaCsv, caminho);

        FalhaLeituraArquivo falha = new(Linha: 13, TipoFalhaLeituraArquivo.ForaDoLayout, Mensagem: string.Empty);
        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Importacao.ArquivoInvalido(falha).Mensagem);
        resultado.Texto.Should().NotContain("TEXTO-DO-ARQUIVO");
    }
}
