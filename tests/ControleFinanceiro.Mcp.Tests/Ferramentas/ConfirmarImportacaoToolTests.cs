using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Mcp.Tests.Infraestrutura;
using ControleFinanceiro.Tests.Shared.Respostas;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace ControleFinanceiro.Mcp.Tests.Ferramentas;

/// <summary>
/// A confirmação da importação pelo processo real, chamada como o cliente a envia depois que o usuário aprova: lê o
/// arquivo de novo e grava só quando o resumo é o da prévia.
/// </summary>
[Collection(ServidorMcpCollection.Nome)]
public sealed class ConfirmarImportacaoToolTests : IAsyncLifetime
{
    private const string NomeFerramenta = "confirmar_importacao";

    private readonly ServidorMcpFixture _servidor;
    private readonly ImportacaoDeTeste _importacao;

    public ConfirmarImportacaoToolTests(ServidorMcpFixture servidor)
    {
        _servidor = servidor;
        _importacao = new ImportacaoDeTeste(servidor);
    }

    public Task InitializeAsync() => _servidor.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // O resumo vem primeiro, para ser o que o usuário lê primeiro no pedido de aprovação.
    [Fact]
    public async Task Esquema_Deve_TerOResumoOCaminhoOLayoutEOSufixo()
    {
        McpClientTool ferramenta = await _servidor.ClienteMcp.ObterFerramentaAsync(NomeFerramenta);

        ferramenta.ProtocolTool.InputSchema
            .GetProperty("properties")
            .EnumerateObject()
            .Select(p => p.Name)
            .Should().Equal("resumo", "caminho", "layout", "sufixoConta");
    }

    [Fact]
    public async Task Meta_Deve_PedirAInteracaoDoUsuario()
    {
        McpClientTool ferramenta = await _servidor.ClienteMcp.ObterFerramentaAsync(NomeFerramenta);

        ferramenta.ProtocolTool.Meta![ServidorMcpFixture.MetaInteracaoDoUsuario]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task Confirmar_Quando_ExtratoComOResumoDaPrevia_Deve_GravarAContaAImportacaoEAsTransacoes()
    {
        await _importacao.ResolverAsync(LayoutArquivoImportacao.C6ContaCsv, ImportacaoDeTeste.ExtratoC6);
        string resumo = await PreverAsync(ImportacaoDeTeste.ExtratoC6, "c6-conta-csv");

        CallToolResult resultado = await _servidor.ClienteMcp.ConfirmarImportacaoAsync(resumo, ImportacaoDeTeste.ExtratoC6, "c6-conta-csv");

        ImportacaoLida importacao = new(resultado.Json);
        importacao.Resumo.Should().Be(resumo);
        (await _importacao.LerGravadoAsync()).Should().Be(
            Gravado.Nada with { Contas = 1, Importacoes = 1, Transacoes = importacao.Novas });
    }

    [Fact]
    public async Task Confirmar_Quando_ExtratoDoMercadoPago_Deve_GravarComOSufixoInformado()
    {
        await _importacao.ResolverAsync(LayoutArquivoImportacao.MercadoPagoContaCsv, ImportacaoDeTeste.ExtratoMercadoPago);
        string resumo = await PreverAsync(ImportacaoDeTeste.ExtratoMercadoPago, "mercado-pago-conta-csv", ImportacaoDeTeste.SufixoContaMercadoPago);

        CallToolResult resultado = await _servidor.ClienteMcp.ConfirmarImportacaoAsync(
            resumo,
            ImportacaoDeTeste.ExtratoMercadoPago,
            "mercado-pago-conta-csv",
            ImportacaoDeTeste.SufixoContaMercadoPago);

        resultado.Json.Should().NotContainKey("erro");
        (await _importacao.LerGravadoAsync()).Contas.Should().Be(1);
    }

    [Fact]
    public async Task Confirmar_Quando_OutroSufixoDaConta_Deve_DevolverResumoDivergenteSemGravar()
    {
        await _importacao.ResolverAsync(LayoutArquivoImportacao.MercadoPagoContaCsv, ImportacaoDeTeste.ExtratoMercadoPago);
        string resumo = await PreverAsync(ImportacaoDeTeste.ExtratoMercadoPago, "mercado-pago-conta-csv", ImportacaoDeTeste.SufixoContaMercadoPago);

        CallToolResult resultado = await _servidor.ClienteMcp.ConfirmarImportacaoAsync(
            resumo,
            ImportacaoDeTeste.ExtratoMercadoPago,
            "mercado-pago-conta-csv",
            sufixoConta: "9999");

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Importacao.ResumoDivergente.Mensagem);
        (await _importacao.LerGravadoAsync()).Should().Be(Gravado.Nada);
    }

    [Theory]
    [InlineData("c6-cartao-csv", ImportacaoDeTeste.FaturaC6)]
    [InlineData("mercado-pago-cartao-csv", ImportacaoDeTeste.FaturaMercadoPago)]
    public async Task Confirmar_Quando_Fatura_Deve_GravarAsTransacoesEAsDespesasParceladasEAdiadas(string layout, string caminho)
    {
        await _importacao.ResolverAsync(LayoutArquivoImportacao.FromValue(layout), caminho);
        string resumo = await PreverAsync(caminho, layout);

        CallToolResult resultado = await _servidor.ClienteMcp.ConfirmarImportacaoAsync(resumo, caminho, layout);

        ImportacaoLida importacao = new(resultado.Json);
        importacao.DespesasParceladas.Should().BePositive();
        importacao.DespesasAdiadas.Should().BePositive();
        (await _importacao.LerGravadoAsync()).Should().Be(new Gravado(
            Contas: 0,
            Importacoes: importacao.Finais.Count,
            Transacoes: importacao.Novas,
            importacao.DespesasParceladas,
            importacao.DespesasAdiadas));
    }

    [Fact]
    public async Task Confirmar_Quando_ResumoAlterado_Deve_DevolverResumoDivergenteSemGravar()
    {
        await _importacao.ResolverAsync(LayoutArquivoImportacao.C6ContaCsv, ImportacaoDeTeste.ExtratoC6);
        string resumo = await PreverAsync(ImportacaoDeTeste.ExtratoC6, "c6-conta-csv");

        CallToolResult resultado = await _servidor.ClienteMcp.ConfirmarImportacaoAsync(
            resumo.Replace("lidas", "lidos", StringComparison.Ordinal),
            ImportacaoDeTeste.ExtratoC6,
            "c6-conta-csv");

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Importacao.ResumoDivergente.Mensagem);
        (await _importacao.LerGravadoAsync()).Should().Be(Gravado.Nada);
    }

    [Fact]
    public async Task Confirmar_Quando_Repetida_Deve_DevolverResumoDivergenteSemGravarDeNovo()
    {
        await _importacao.ResolverAsync(LayoutArquivoImportacao.C6ContaCsv, ImportacaoDeTeste.ExtratoC6);
        string resumo = await PreverAsync(ImportacaoDeTeste.ExtratoC6, "c6-conta-csv");
        await _servidor.ClienteMcp.ConfirmarImportacaoAsync(resumo, ImportacaoDeTeste.ExtratoC6, "c6-conta-csv");
        Gravado depoisDaPrimeira = await _importacao.LerGravadoAsync();

        CallToolResult resultado = await _servidor.ClienteMcp.ConfirmarImportacaoAsync(resumo, ImportacaoDeTeste.ExtratoC6, "c6-conta-csv");

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Importacao.ResumoDivergente.Mensagem);
        (await _importacao.LerGravadoAsync()).Should().Be(depoisDaPrimeira);
    }

    [Fact]
    public async Task Prever_Quando_ArquivoJaImportado_Deve_ContarTodasComoJaExistentes()
    {
        await _importacao.ResolverAsync(LayoutArquivoImportacao.C6ContaCsv, ImportacaoDeTeste.ExtratoC6);
        string resumo = await PreverAsync(ImportacaoDeTeste.ExtratoC6, "c6-conta-csv");
        await _servidor.ClienteMcp.ConfirmarImportacaoAsync(resumo, ImportacaoDeTeste.ExtratoC6, "c6-conta-csv");

        CallToolResult resultado = await _servidor.ClienteMcp.PreverImportacaoAsync(ImportacaoDeTeste.ExtratoC6, "c6-conta-csv");

        ImportacaoLida importacao = new(resultado.Json);
        importacao.Novas.Should().Be(0);
        importacao.JaExistentes.Should().Be(importacao.Lidas);
        importacao.ContaNova.Should().BeFalse();
    }

    private async Task<string> PreverAsync(string caminho, string layout, string? sufixoConta = null)
    {
        CallToolResult previa = await _servidor.ClienteMcp.PreverImportacaoAsync(caminho, layout, sufixoConta);
        return new ImportacaoLida(previa.Json).Resumo;
    }
}
