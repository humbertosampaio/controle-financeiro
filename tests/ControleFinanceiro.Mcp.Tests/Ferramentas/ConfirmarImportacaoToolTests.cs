using AutoFixture;
using ControleFinanceiro.Application.IA;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Mcp.Tests.Infraestrutura;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using ControleFinanceiro.Tests.Shared.Respostas;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text.Json;

namespace ControleFinanceiro.Mcp.Tests.Ferramentas;

/// <summary>
/// A confirmação da importação pelo processo real, chamada como o cliente a envia depois que o usuário aprova: lê o
/// arquivo de novo e grava só quando o resumo é o da prévia.
/// </summary>
[Collection(ServidorMcpCollection.Nome)]
public sealed class ConfirmarImportacaoToolTests : IAsyncLifetime
{
    private const decimal ValorBoleto = 230.45m;

    private static readonly DateOnly DiaBoleto = new(2026, 3, 3);
    private static readonly VencimentoFatura VencimentoPagoPeloBoleto = (VencimentoFatura)new DateOnly(2026, 3, 10);

    private readonly ServidorMcpFixture _servidor;
    private readonly ImportacaoDeTeste _importacao;
    private readonly PagamentoFaturaDeTeste _fatura;

    public ConfirmarImportacaoToolTests(ServidorMcpFixture servidor)
    {
        _servidor = servidor;
        _importacao = new ImportacaoDeTeste(servidor);
        _fatura = new PagamentoFaturaDeTeste(servidor);
    }

    public Task InitializeAsync() => _servidor.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // O resumo vem primeiro, para ser o que o usuário lê primeiro no pedido de aprovação.
    [Fact]
    public async Task Esquema_Deve_TerOResumoOCaminhoOLayoutEOSufixo()
    {
        McpClientTool ferramenta = await _servidor.ClienteMcp.ObterFerramentaAsync(Tools.ConfirmarImportacao);

        ferramenta.ProtocolTool.InputSchema
            .GetProperty("properties")
            .EnumerateObject()
            .Select(p => p.Name)
            .Should().Equal("resumo", "caminho", "layout", "sufixoConta", "vencimento");
    }

    // Sem o tipo, o cliente manda o sufixo e o vencimento como número, e o SDK não os converte para texto.
    [Theory]
    [InlineData("sufixoConta")]
    [InlineData("vencimento")]
    public async Task Esquema_Deve_TerOParametroComoTextoOpcional(string parametro)
    {
        McpClientTool ferramenta = await _servidor.ClienteMcp.ObterFerramentaAsync(Tools.ConfirmarImportacao);

        JsonElement esquema = ferramenta.ProtocolTool.InputSchema;
        esquema.GetProperty("properties").GetProperty(parametro).GetProperty("type").GetString().Should().Be("string");
        esquema.GetProperty("required").EnumerateArray().Select(obrigatorio => obrigatorio.GetString()).Should().NotContain(parametro);
    }

    [Fact]
    public async Task Meta_Deve_PedirAInteracaoDoUsuario()
    {
        McpClientTool ferramenta = await _servidor.ClienteMcp.ObterFerramentaAsync(Tools.ConfirmarImportacao);

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

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Importacao.ResumoDivergente(LayoutArquivoImportacao.MercadoPagoContaCsv).Mensagem);
        (await _importacao.LerGravadoAsync()).Should().Be(Gravado.Nada);
    }

    [Theory]
    [InlineData("c6-cartao-csv", ImportacaoDeTeste.FaturaC6)]
    [InlineData("mercado-pago-cartao-csv", ImportacaoDeTeste.FaturaMercadoPago)]
    public async Task Confirmar_Quando_Fatura_Deve_GravarAsTransacoesEAsDespesasParceladasEAdiadas(string layout, string caminho)
    {
        await _importacao.ResolverAsync(LayoutArquivoImportacao.FromValue(layout), caminho);
        string? vencimento = ImportacaoDeTeste.VencimentoDoLayout(layout);
        string resumo = await PreverAsync(caminho, layout, vencimento: vencimento);

        CallToolResult resultado = await _servidor.ClienteMcp.ConfirmarImportacaoAsync(resumo, caminho, layout, vencimento: vencimento);

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
    public async Task Confirmar_Quando_FaturaSemPagamento_Deve_GravarETrazerOAviso()
    {
        string caminho = ImportacaoDeTeste.GravarComLinhaTrocada(
            ImportacaoDeTeste.FaturaC6,
            numero: 6,
            "02/03/2026;FULANA DE TAL FICTICIA;1111;-;CREDITO FICTICIO;Única;0;0;-500.00");
        await _importacao.ResolverAsync(LayoutArquivoImportacao.C6CartaoCsv, caminho);
        string resumo = await PreverAsync(caminho, "c6-cartao-csv", vencimento: ImportacaoDeTeste.VencimentoFaturaC6);

        CallToolResult resultado = await _servidor.ClienteMcp.ConfirmarImportacaoAsync(
            resumo,
            caminho,
            "c6-cartao-csv",
            vencimento: ImportacaoDeTeste.VencimentoFaturaC6);

        ImportacaoLida importacao = new(resultado.Json);
        importacao.Avisos.Should().Equal("A fatura não traz o pagamento da fatura anterior: se não é a primeira fatura do cartão, confira se o layout do banco mudou.");
        (await _importacao.LerGravadoAsync()).Transacoes.Should().Be(importacao.Novas).And.BePositive();
    }

    [Fact]
    public async Task Confirmar_Quando_OutroVencimento_Deve_DevolverResumoDivergenteSemGravar()
    {
        await _importacao.ResolverAsync(LayoutArquivoImportacao.C6CartaoCsv, ImportacaoDeTeste.FaturaC6);
        string resumo = await PreverAsync(ImportacaoDeTeste.FaturaC6, "c6-cartao-csv", vencimento: ImportacaoDeTeste.VencimentoFaturaC6);

        CallToolResult resultado = await _servidor.ClienteMcp.ConfirmarImportacaoAsync(
            resumo,
            ImportacaoDeTeste.FaturaC6,
            "c6-cartao-csv",
            vencimento: "2026-04-12");

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Importacao.ResumoDivergente(LayoutArquivoImportacao.C6CartaoCsv).Mensagem);
        (await _importacao.LerGravadoAsync()).Should().Be(Gravado.Nada);
    }

    [Fact]
    public async Task Confirmar_Quando_FaturaJaImportadaComOutroVencimentoNoMesmoMes_Deve_DevolverFaturaJaImportadaSemGravar()
    {
        await _importacao.ResolverAsync(LayoutArquivoImportacao.C6CartaoCsv, ImportacaoDeTeste.FaturaC6);
        string resumo = await PreverAsync(ImportacaoDeTeste.FaturaC6, "c6-cartao-csv", vencimento: ImportacaoDeTeste.VencimentoFaturaC6);
        await _servidor.ClienteMcp.ConfirmarImportacaoAsync(resumo, ImportacaoDeTeste.FaturaC6, "c6-cartao-csv", vencimento: ImportacaoDeTeste.VencimentoFaturaC6);
        Gravado gravadoAposPrimeira = await _importacao.LerGravadoAsync();

        CallToolResult resultado = await _servidor.ClienteMcp.ConfirmarImportacaoAsync(
            resumo,
            ImportacaoDeTeste.FaturaC6,
            "c6-cartao-csv",
            vencimento: "2026-04-13");

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Importacao.FaturaJaImportadaComOutroVencimento.Mensagem);
        (await _importacao.LerGravadoAsync()).Should().Be(gravadoAposPrimeira);
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

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Importacao.ResumoDivergente(LayoutArquivoImportacao.C6ContaCsv).Mensagem);
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

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Importacao.ResumoDivergente(LayoutArquivoImportacao.C6ContaCsv).Mensagem);
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

    [Theory, DomainAutoData]
    public async Task Confirmar_Quando_FaturaComOTotalDeUmaSaidaDoExtrato_Deve_ApontarASaida(Fixture fixture)
    {
        Cartao cartao = await GravarFaturaPagaPeloBoletoAsync(fixture);
        await _importacao.ResolverAsync(LayoutArquivoImportacao.C6ContaCsv, ImportacaoDeTeste.ExtratoC6);
        string resumo = await PreverAsync(ImportacaoDeTeste.ExtratoC6, "c6-conta-csv");

        CallToolResult resultado = await _servidor.ClienteMcp.ConfirmarImportacaoAsync(resumo, ImportacaoDeTeste.ExtratoC6, "c6-conta-csv");

        CandidatoPagamentoFaturaLido candidato = new ImportacaoLida(resultado.Json).CandidatosPagamentoFatura.Should().ContainSingle().Subject;
        candidato.Transacao.Data.Should().Be(DiaBoleto);
        candidato.IdCartao.Should().Be(cartao.Id.Value);
        candidato.Vencimento.Should().Be(VencimentoPagoPeloBoleto);
        candidato.Total.Should().Be(ValorBoleto);
    }

    // As consultas não enxergam o que a prévia só aplicou em memória: o candidato aparece na confirmação.
    [Theory, DomainAutoData]
    public async Task Prever_Quando_FaturaComOTotalDeUmaSaidaDoExtrato_Deve_NaoTrazerCandidatos(Fixture fixture)
    {
        await GravarFaturaPagaPeloBoletoAsync(fixture);
        await _importacao.ResolverAsync(LayoutArquivoImportacao.C6ContaCsv, ImportacaoDeTeste.ExtratoC6);

        CallToolResult previa = await _servidor.ClienteMcp.PreverImportacaoAsync(ImportacaoDeTeste.ExtratoC6, "c6-conta-csv");

        previa.Json.Should().NotContainKey("candidatos_pagamento_fatura");
    }

    /// <summary>
    /// A fatura de um cartão, com o vencimento de 10/03/2026 e o total de 230,45: o valor do pagamento de boleto do extrato
    /// fictício do C6, em 03/03/2026, dentro da janela.
    /// </summary>
    private async Task<Cartao> GravarFaturaPagaPeloBoletoAsync(Fixture fixture)
    {
        fixture.Inject(_servidor.Usuario.Id);
        Cartao cartao = fixture.Create<Cartao>();
        await _fatura.GravarFaturaImportadaAsync(fixture, cartao, VencimentoPagoPeloBoleto, -ValorBoleto);
        return cartao;
    }

    private async Task<string> PreverAsync(string caminho, string layout, string? sufixoConta = null, string? vencimento = null)
    {
        CallToolResult previa = await _servidor.ClienteMcp.PreverImportacaoAsync(caminho, layout, sufixoConta, vencimento);
        return new ImportacaoLida(previa.Json).Resumo;
    }
}
