using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Mcp.Tests.Infraestrutura;
using ControleFinanceiro.Tests.Shared.Respostas;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text.Json;

namespace ControleFinanceiro.Mcp.Tests.Ferramentas;

/// <summary>
/// A prévia da importação pelo processo real, com as fixtures fictícias na pasta de origem temporária do servidor: ela
/// conta, mas nunca grava, e nunca manda ao modelo uma linha do arquivo.
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

    // Sem o tipo, o cliente manda o sufixo como número, e o SDK não o converte para texto.
    [Fact]
    public async Task Esquema_Deve_TerOSufixoComoTextoOpcional()
    {
        McpClientTool ferramenta = await _servidor.ClienteMcp.ObterFerramentaAsync("prever_importacao");

        JsonElement esquema = ferramenta.ProtocolTool.InputSchema;
        esquema.GetProperty("properties").GetProperty("sufixoConta").GetProperty("type").GetString().Should().Be("string");
        esquema.GetProperty("required").EnumerateArray().Select(obrigatorio => obrigatorio.GetString()).Should().NotContain("sufixoConta");
    }

    [Theory, MemberData(nameof(Arquivos))]
    public async Task Prever_Quando_CadastrosResolvidos_Deve_ContarOsLancamentosSemGravar(string layout, string caminho)
    {
        await _importacao.ResolverAsync(LayoutArquivoImportacao.FromValue(layout), caminho);

        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(caminho, layout, Sufixo(layout));

        IReadOnlyList<Lancamento> lancamentos = await ImportacaoDeTeste.LerLancamentosAsync(LayoutArquivoImportacao.FromValue(layout), caminho);
        ImportacaoLida importacao = new(resultado.Json);
        importacao.Lidas.Should().Be(lancamentos.Count);
        importacao.Novas.Should().Be(lancamentos.Count);
        (await _importacao.LerGravadoAsync()).Should().Be(Gravado.Nada);
    }

    [Theory, MemberData(nameof(Arquivos))]
    public async Task Prever_Deve_NaoMandarADescricaoNemAContraparteDoArquivo(string layout, string caminho)
    {
        await _importacao.ResolverAsync(LayoutArquivoImportacao.FromValue(layout), caminho);

        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(caminho, layout, Sufixo(layout));

        IReadOnlyList<Lancamento> lancamentos = await ImportacaoDeTeste.LerLancamentosAsync(LayoutArquivoImportacao.FromValue(layout), caminho);
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

        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(ImportacaoDeTeste.ExtratoC6, "c6-conta-csv");

        string numeroConta = ImportacaoDeTeste.NumeroContaDoCabecalhoC6();
        resultado.Texto.Should().NotContain(numeroConta).And.Contain($"conta final {numeroConta[^4..]}");
    }

    [Fact]
    public async Task Prever_Quando_ContaNaoCadastrada_Deve_DizerQueCriaAConta()
    {
        await _importacao.ResolverAsync(LayoutArquivoImportacao.C6ContaCsv, ImportacaoDeTeste.ExtratoC6);

        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(ImportacaoDeTeste.ExtratoC6, "c6-conta-csv");

        new ImportacaoLida(resultado.Json).ContaNova.Should().BeTrue();
    }

    [Fact]
    public async Task Prever_Quando_CaminhoAbsolutoNaOrigem_Deve_LerOArquivo()
    {
        await _importacao.ResolverAsync(LayoutArquivoImportacao.C6ContaCsv, ImportacaoDeTeste.ExtratoC6);

        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(
            ImportacaoDeTeste.Completo(ImportacaoDeTeste.ExtratoC6),
            "c6-conta-csv");

        resultado.Json.Should().NotContainKey("erro");
    }

    [Fact]
    public async Task Prever_Quando_FinalSemCartao_Deve_DevolverOsFinaisSemGravar()
    {
        await _importacao.ResolverAliasesAsync(LayoutArquivoImportacao.C6CartaoCsv, ImportacaoDeTeste.FaturaC6);

        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(ImportacaoDeTeste.FaturaC6, "c6-cartao-csv");

        Fatura fatura = await ImportacaoDeTeste.LerFaturaAsync(LayoutArquivoImportacao.C6CartaoCsv, ImportacaoDeTeste.FaturaC6);
        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Importacao.CartoesNaoResolvidos.Mensagem);
        PendenciasImportacaoLidas.DoEnvelope(resultado.Json).FinaisSemCartao.Should().Equal(fatura.Sufixos.Select(sufixo => (string)sufixo));
        (await _importacao.LerGravadoAsync()).Should().Be(Gravado.Nada);
    }

    [Fact]
    public async Task Prever_Quando_FaturaSemPagamento_Deve_TrazerOAviso()
    {
        string caminho = ImportacaoDeTeste.GravarComLinhaTrocada(
            ImportacaoDeTeste.FaturaC6,
            numero: 6,
            "02/03/2026;FULANA DE TAL FICTICIA;1111;-;CREDITO FICTICIO;Única;0;0;-500.00");
        await _importacao.ResolverAsync(LayoutArquivoImportacao.C6CartaoCsv, caminho);

        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(caminho, "c6-cartao-csv");

        new ImportacaoLida(resultado.Json).Avisos.Should().ContainSingle()
            .Which.Should().Be(Fatura.AvisoSemPagamentoAnterior);
    }

    [Fact]
    public async Task Prever_Quando_FaturaComPagamento_Deve_NaoTrazerAviso()
    {
        await _importacao.ResolverAsync(LayoutArquivoImportacao.C6CartaoCsv, ImportacaoDeTeste.FaturaC6);

        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(ImportacaoDeTeste.FaturaC6, "c6-cartao-csv");

        new ImportacaoLida(resultado.Json).Avisos.Should().BeEmpty();
    }

    [Fact]
    public async Task Prever_Quando_BeneficiarioSemAlias_Deve_DevolverAQuantidade()
    {
        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(ImportacaoDeTeste.ExtratoC6, "c6-conta-csv");

        IReadOnlyList<Lancamento> lancamentos = await ImportacaoDeTeste.LerLancamentosAsync(LayoutArquivoImportacao.C6ContaCsv, ImportacaoDeTeste.ExtratoC6);
        int pendentes = AliasesBeneficiarioPendentes.Listar(_servidor.Usuario.Id, beneficiariosDoUsuario: [], lancamentos).Count;
        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Importacao.BeneficiariosNaoResolvidos.Mensagem);
        PendenciasImportacaoLidas.DoEnvelope(resultado.Json).Beneficiarios.Should().Be(pendentes).And.BePositive();
    }

    [Fact]
    public async Task Prever_Quando_LayoutDesconhecido_Deve_DevolverLayoutInvalido()
    {
        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(ImportacaoDeTeste.ExtratoC6, "outro-layout");

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Importacao.LayoutInvalido.Mensagem);
    }

    [Fact]
    public async Task Prever_Quando_ExtratoDoMercadoPagoSemSufixo_Deve_DevolverSufixoContaInvalido()
    {
        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(ImportacaoDeTeste.ExtratoMercadoPago, "mercado-pago-conta-csv");

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Importacao.SufixoContaInvalido.Mensagem);
    }

    [Fact]
    public async Task Prever_Quando_CaminhoSaiDaOrigem_Deve_DevolverArquivoForaDaOrigem()
    {
        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync("dados/../fora.csv", "c6-conta-csv");

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Importacao.ArquivoForaDaOrigem.Mensagem);
    }

    [Fact]
    public async Task Prever_Quando_ArquivoNaoExiste_Deve_DevolverArquivoNaoEncontrado()
    {
        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync("dados/inexistente.csv", "c6-conta-csv");

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Importacao.ArquivoNaoEncontrado.Mensagem);
    }

    [Fact]
    public async Task Prever_Quando_LinhaForaDoLayout_Deve_DevolverSoONumeroDaLinha()
    {
        string caminho = ImportacaoDeTeste.GravarComLinhaTrocada(ImportacaoDeTeste.ExtratoC6, numero: 13, "03/03/2026,03/03/2026,TEXTO-DO-ARQUIVO,,0.00,5.00");

        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(caminho, "c6-conta-csv");

        FalhaLeituraArquivo falha = new(Linha: 13, TipoFalhaLeituraArquivo.ForaDoLayout, Mensagem: string.Empty);
        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Importacao.ArquivoInvalido(falha).Mensagem);
        resultado.Texto.Should().NotContain("TEXTO-DO-ARQUIVO");
    }

    private static string? Sufixo(string layout) =>
        layout == LayoutArquivoImportacao.MercadoPagoContaCsv.Value ? ImportacaoDeTeste.SufixoContaMercadoPago : null;
}
