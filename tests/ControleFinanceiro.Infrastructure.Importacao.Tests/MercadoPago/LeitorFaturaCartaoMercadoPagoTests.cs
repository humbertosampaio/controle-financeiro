using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Infrastructure.Importacao.MercadoPago;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Infrastructure.Importacao.Tests.MercadoPago;

/// <summary>
/// Lê a fatura fictícia em <c>Fixtures/MercadoPago/fatura-cartao.csv</c>, no layout do prompt de
/// <c>docs/fatura-mercado-pago.md</c>. Os casos de erro trocam campos de uma linha dela: o número da linha no teste é o
/// do arquivo, contando a partir de 1.
/// </summary>
public sealed class LeitorFaturaCartaoMercadoPagoTests
{
    private const int LinhaColunas = 1;
    private const int LinhaParcelada = 2;
    private const int LinhaCompraVista = 3;
    private const int LinhaPagamento = 8;
    private const string Cabecalho = "mes_referencia;vencimento;fechamento;data_compra;data_lancamento;tipo_lancamento;descricao_fatura;vendedor;produtos;numero_pedido;numero_transacao;cartao_final;parcela_atual;parcelas_totais;moeda;valor_compra_original;valor_cobrado_fatura;valor_creditado_fatura;status";

    private static readonly string[] _colunas = Cabecalho.Split(';');

    /// <summary>O arquivo como o Mago o entregou numa das faturas: UTF-8 sem BOM e CRLF.</summary>
    private static readonly ArquivoFixture _fixture = new("MercadoPago", "fatura-cartao.csv", fimLinha: "\r\n", comBom: false);
    private static readonly string _caminhoFaturaC6 = Path.Combine(AppContext.BaseDirectory, "Fixtures", "C6", "fatura-cartao.csv");
    private static readonly string _caminhoExtrato = Path.Combine(AppContext.BaseDirectory, "Fixtures", "MercadoPago", "extrato-conta.csv");
    private static readonly SufixoCartao _final = (SufixoCartao)"1234";

    public static readonly TheoryData<string> ArquivosDeOutroLayout = new(_caminhoFaturaC6, _caminhoExtrato);

    private readonly LeitorFaturaCartaoMercadoPago _leitor = new();

    [Fact]
    public async Task LerAsync_Deve_LerOBancoEOFinal()
    {
        Fatura fatura = await LerFixtureAsync();

        fatura.Banco.Should().Be(Banco.MercadoPago);
        fatura.Sufixos.Should().Equal(_final);
    }

    [Fact]
    public async Task LerAsync_Deve_LerOsLancamentosNaOrdemDoArquivo()
    {
        Fatura fatura = await LerFixtureAsync();

        fatura.Lancamentos.Select(l => l.Lancamento.Descricao).Should().Equal(
            [
                (TextoObrigatorio)"LOJA FICTICIA ELETRO",
                (TextoObrigatorio)"PADARIA FICTICIA",
                (TextoObrigatorio)"MERCADO FICTICIO",
                (TextoObrigatorio)"LIVRARIA \"FICTICIA\"",
                (TextoObrigatorio)"CAFE FICTICIO",
                (TextoObrigatorio)"CAFE FICTICIO",
                (TextoObrigatorio)"Fatura de abril",
            ]);
    }

    [Fact]
    public async Task LerAsync_Quando_FimLinhaLf_Deve_LerAMesmaFatura()
    {
        Fatura crlf = await LerFixtureAsync();
        Result<Fatura> result = await _leitor.LerAsync(_fixture.ComFimLinha("\n"), CancellationToken.None);

        result.Should().Succeed().Which.Lancamentos.Should().Equal(crlf.Lancamentos);
    }

    [Fact]
    public async Task LerAsync_Deve_LerACompraNegativaEOPagamentoPositivo()
    {
        Fatura fatura = await LerFixtureAsync();

        fatura.Lancamentos.Select(l => l.Lancamento.Valor).Should().Equal(
            [-100.00m, -25.90m, -1234.56m, -49.90m, -9.50m, -9.50m, 1429.36m]);
    }

    [Fact]
    public async Task LerAsync_Deve_LerADataDaCompraEADoLancamentoQuandoNaoHaCompra()
    {
        Fatura fatura = await LerFixtureAsync();

        fatura.Lancamentos.Select(l => l.Lancamento.Data).Should().Equal(
            new DateOnly(2025, 11, 15),
            new DateOnly(2026, 3, 5),
            new DateOnly(2026, 3, 8),
            new DateOnly(2026, 3, 12),
            new DateOnly(2026, 3, 20),
            new DateOnly(2026, 3, 20),
            new DateOnly(2026, 4, 10));
    }

    [Fact]
    public async Task LerAsync_Quando_CompraParcelada_Deve_LerAParcela()
    {
        Fatura fatura = await LerFixtureAsync();

        fatura.Lancamentos[0].Lancamento.Parcela.Should().Be((Parcela)(3, 10));
        fatura.Lancamentos.Skip(1).Should().AllSatisfy(l => l.Lancamento.Parcela.Should().BeNull());
    }

    [Fact]
    public async Task LerAsync_Quando_ParcelaUmDeUm_Deve_DeixarSemParcela()
    {
        Stream arquivo = ComCampos(LinhaParcelada, ("parcela_atual", "1"), ("parcelas_totais", "1"));

        Fatura fatura = (await _leitor.LerAsync(arquivo, CancellationToken.None)).Value;

        fatura.Lancamentos[0].Lancamento.Parcela.Should().BeNull();
    }

    [Fact]
    public async Task LerAsync_Deve_TravarAIdentidadeCompostaNoNumeroNaParcelaENoValor()
    {
        Fatura fatura = await LerFixtureAsync();

        fatura.Lancamentos[0].Lancamento.Identidade.Should().Be(
            IdentidadeLancamento.Composta("100000000001").Value.ComParcela((Parcela)(3, 10)).ComValor(-100.00m));
        fatura.Lancamentos[6].Lancamento.Identidade.Should().Be(
            IdentidadeLancamento.Composta("100000000007_1").Value.ComParcela(parcela: null).ComValor(1429.36m));
    }

    [Fact]
    public async Task LerAsync_Quando_CompraDeValorZero_Deve_LerZeroSemSinal()
    {
        Stream arquivo = ComCampos(LinhaCompraVista, ("valor_cobrado_fatura", "0,00"));

        Fatura fatura = (await _leitor.LerAsync(arquivo, CancellationToken.None)).Value;

        Reais valor = fatura.Lancamentos[1].Lancamento.Valor;
        valor.Should().Be((Reais)0m);
        decimal.IsNegative(valor).Should().BeFalse();
    }

    [Fact]
    public async Task LerAsync_Quando_ValorComQuinzeDigitosInteiros_Deve_LerOValor()
    {
        Stream arquivo = ComCampos(LinhaCompraVista, ("valor_cobrado_fatura", "999999999999999,99"));

        Fatura fatura = (await _leitor.LerAsync(arquivo, CancellationToken.None)).Value;

        fatura.Lancamentos[1].Lancamento.Valor.Should().Be((Reais)(-999999999999999.99m));
    }

    [Fact]
    public async Task LerAsync_Quando_ComprasIguaisComNumerosDiferentes_Deve_TerIdentidadesDiferentes()
    {
        Fatura fatura = await LerFixtureAsync();

        fatura.Lancamentos[4].Lancamento.Identidade.Should().NotBe(fatura.Lancamentos[5].Lancamento.Identidade);
    }

    [Fact]
    public async Task LerAsync_Quando_PagamentoSemFinal_Deve_PorNoFinalDaFatura()
    {
        Fatura fatura = await LerFixtureAsync();

        fatura.Lancamentos[6].SufixoCartao.Should().Be(_final);
    }

    [Fact]
    public async Task LerAsync_Quando_DoisFinais_Deve_PorOPagamentoNoMenor()
    {
        Stream arquivo = ComCampos(LinhaCompraVista, ("cartao_final", "0042"));

        Fatura fatura = (await _leitor.LerAsync(arquivo, CancellationToken.None)).Value;

        fatura.Lancamentos.Select(l => l.SufixoCartao).Should().Equal(
            _final, (SufixoCartao)"0042", _final, _final, _final, _final, (SufixoCartao)"0042");
    }

    [Fact]
    public async Task LerAsync_Deve_ManterOsDocumentosDoVendedorNaContraparte()
    {
        Fatura fatura = await LerFixtureAsync();

        fatura.Lancamentos.Select(l => l.Lancamento.Contraparte).Should().Equal(
            null,
            "FULANO FICTICIO 123.456.789-09",
            "MERCADO FICTICIO LTDA 11.222.333/0001-81",
            null,
            null,
            null,
            null);
    }

    [Fact]
    public async Task LerAsync_Deve_DeixarOsProdutosForaDeTodoLancamento()
    {
        Fatura fatura = await LerFixtureAsync();

        string[] proibidos = ["12.ABC.345/01DE-35", "Ficticio", "2000000000000001"];
        fatura.Lancamentos.Should().AllSatisfy(l =>
        {
            Lancamento lancamento = l.Lancamento;
            string?[] textos = [lancamento.Descricao, lancamento.Contraparte, lancamento.CategoriaBanco, .. lancamento.Identidade.Campos];
            textos.Should().NotContain(t => t != null && proibidos.Any(p => t.Contains(p, StringComparison.Ordinal)));
        });
    }

    [Fact]
    public async Task LerAsync_Quando_DescricaoSoComDocumento_Deve_ManterODocumentoNaDescricao()
    {
        Stream arquivo = ComCampos(LinhaCompraVista, ("descricao_fatura", "123.456.789-09"));

        Result<Fatura> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().Succeed().Which.Lancamentos.Should().Contain(l => l.Lancamento.Descricao == (TextoObrigatorio)"123.456.789-09");
    }

    [Theory]
    [InlineData("tipo_lancamento", "estorno", "não tem um tipo de lançamento conhecido.")]
    [InlineData("tipo_lancamento", "", "não tem um tipo de lançamento conhecido.")]
    [InlineData("valor_cobrado_fatura", "", "não traz o valor na coluna do tipo de lançamento.")]
    [InlineData("moeda", "USD", "não traz os valores em reais (BRL).")]
    [InlineData("data_compra", "", "não tem a data da compra nem a do lançamento.")]
    [InlineData("cartao_final", "", "não tem o final do cartão com 4 dígitos.")]
    [InlineData("cartao_final", "12A4", "não tem o final do cartão com 4 dígitos.")]
    [InlineData("descricao_fatura", "  ", "não tem descrição.")]
    [InlineData("numero_transacao", " ", "não tem o número da transação.")]
    public async Task LerAsync_Quando_CampoInvalidoNaCompra_Deve_RetornarFalhaSoComONumeroDaLinha(string coluna, string valor, string erro)
    {
        Stream arquivo = ComCampos(LinhaCompraVista, (coluna, valor));

        Result<Fatura> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith($"A linha 3 do arquivo {erro}");
    }

    [Fact]
    public async Task LerAsync_Quando_ValorNasDuasColunas_Deve_RetornarFalhaSoComONumeroDaLinha()
    {
        Stream arquivo = ComCampos(LinhaCompraVista, ("valor_creditado_fatura", "25,90"));

        Result<Fatura> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("A linha 3 do arquivo não traz o valor na coluna do tipo de lançamento.");
    }

    [Fact]
    public async Task LerAsync_Quando_PagamentoComOValorNaColunaDaCobranca_Deve_RetornarFalhaSoComONumeroDaLinha()
    {
        Stream arquivo = ComCampos(LinhaPagamento, ("valor_cobrado_fatura", "1429,36"), ("valor_creditado_fatura", ""));

        Result<Fatura> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("A linha 8 do arquivo não traz o valor na coluna do tipo de lançamento.");
    }

    [Fact]
    public async Task LerAsync_Quando_PagamentoSemNenhumaData_Deve_RetornarFalhaSoComONumeroDaLinha()
    {
        Stream arquivo = ComCampos(LinhaPagamento, ("data_lancamento", ""));

        Result<Fatura> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("A linha 8 do arquivo não tem a data da compra nem a do lançamento.");
    }

    [Theory]
    [InlineData("", "10")]
    [InlineData("3", "")]
    [InlineData("11", "10")]
    [InlineData("0", "10")]
    [InlineData("3 de 10", "10")]
    public async Task LerAsync_Quando_CompraParceladaSemParcelaValida_Deve_RetornarFalhaSoComONumeroDaLinha(string atual, string total)
    {
        Stream arquivo = ComCampos(LinhaParcelada, ("parcela_atual", atual), ("parcelas_totais", total));

        Result<Fatura> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("A linha 2 do arquivo não tem a parcela da compra parcelada.");
    }

    [Theory]
    [InlineData("valor_cobrado_fatura", "25.90")]
    [InlineData("valor_cobrado_fatura", "-25,90")]
    [InlineData("valor_cobrado_fatura", "1.025,90")]
    [InlineData("valor_cobrado_fatura", "25,9")]
    [InlineData("valor_cobrado_fatura", "1000000000000000,00")]
    [InlineData("data_compra", "31/02/2026")]
    [InlineData("data_compra", "2026-03-05")]
    public async Task LerAsync_Quando_CampoForaDoFormato_Deve_RetornarFalhaSoComONumeroDaLinha(string coluna, string valor)
    {
        Stream arquivo = ComCampos(LinhaCompraVista, (coluna, valor));

        Result<Fatura> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("A linha 3 do arquivo não está no layout da fatura do cartão do Mercado Pago.");
    }

    [Theory]
    [InlineData(";\"extra\"")]
    [InlineData(null)]
    public async Task LerAsync_Quando_LinhaComCamposAMaisOuAMenos_Deve_RetornarFalhaSoComONumeroDaLinha(string? acrescimo)
    {
        string linha = _fixture.Linhas()[LinhaCompraVista - 1];
        string trocada = acrescimo is null ? linha[..linha.LastIndexOf(';')] : linha + acrescimo;

        Result<Fatura> result = await _leitor.LerAsync(_fixture.ComLinha(LinhaCompraVista, trocada), CancellationToken.None);

        result.Should().FailWith("A linha 3 do arquivo não está no layout da fatura do cartão do Mercado Pago.");
    }

    [Fact]
    public async Task LerAsync_Quando_ColunasEmOutraCaixa_Deve_RetornarFalhaQueCitaOPrompt()
    {
        Stream arquivo = _fixture.ComLinha(LinhaColunas, _fixture.Linhas().First().ToUpperInvariant());

        Result<Fatura> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("A linha 1 do arquivo não traz as colunas do prompt da fatura do cartão do Mercado Pago (docs/fatura-mercado-pago.md).");
    }

    [Fact]
    public async Task LerAsync_Quando_LinhaDeSeparadorAntesDasColunas_Deve_RetornarFalhaQueCitaOPrompt()
    {
        Stream arquivo = _fixture.ComLinhas(_fixture.Linhas().Prepend("sep=;"));

        Result<Fatura> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("A linha 1 do arquivo não traz as colunas do prompt da fatura do cartão do Mercado Pago (docs/fatura-mercado-pago.md).");
    }

    [Fact]
    public async Task LerAsync_Quando_LinhaForaDoLayoutComQuebraDeLinha_Deve_CitarALinhaOndeORegistroComeca()
    {
        string comColunaAMais = "\"COLUNA\nA MAIS\";" + _fixture.Linhas().ElementAt(LinhaCompraVista - 1);

        Result<Fatura> result = await _leitor.LerAsync(_fixture.ComLinha(LinhaCompraVista, comColunaAMais), CancellationToken.None);

        result.Should().FailWith("A linha 3 do arquivo não está no layout da fatura do cartão do Mercado Pago.");
    }

    [Theory]
    [InlineData("\"data_compra\";\"descricao_fatura\";\"valor_cobrado_fatura\"")]
    [InlineData("\"vencimento\";\"mes_referencia\";\"fechamento\";\"data_compra\";\"data_lancamento\";\"tipo_lancamento\";\"descricao_fatura\";\"vendedor\";\"produtos\";\"numero_pedido\";\"numero_transacao\";\"cartao_final\";\"parcela_atual\";\"parcelas_totais\";\"moeda\";\"valor_compra_original\";\"valor_cobrado_fatura\";\"valor_creditado_fatura\";\"status\"")]
    [InlineData(Cabecalho + ";extra")]
    public async Task LerAsync_Quando_ColunasDiferentesDoPrompt_Deve_RetornarFalhaQueCitaOPrompt(string colunas)
    {
        Stream arquivo = _fixture.ComLinha(LinhaColunas, colunas);

        Result<Fatura> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith(
            "A linha 1 do arquivo não traz as colunas do prompt da fatura do cartão do Mercado Pago (docs/fatura-mercado-pago.md).");
    }

    [Theory]
    [MemberData(nameof(ArquivosDeOutroLayout))]
    public async Task LerAsync_Quando_ArquivoDeOutroLayout_Deve_RetornarFalhaQueCitaOPrompt(string caminho)
    {
        await using FileStream arquivo = File.OpenRead(caminho);

        Result<Fatura> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith(
            "A linha 1 do arquivo não traz as colunas do prompt da fatura do cartão do Mercado Pago (docs/fatura-mercado-pago.md).");
    }

    [Fact]
    public async Task LerAsync_Quando_ArquivoVazio_Deve_RetornarFalha()
    {
        Result<Fatura> result = await _leitor.LerAsync(_fixture.ComLinhas(linhas: []), CancellationToken.None);

        result.Should().FailWith("O arquivo termina antes das colunas da fatura do cartão do Mercado Pago.");
    }

    [Fact]
    public async Task LerAsync_Quando_SoAsColunas_Deve_RetornarFalha()
    {
        Stream arquivo = _fixture.ComLinhas(_fixture.Linhas().Take(LinhaColunas));

        Result<Fatura> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("A fatura não traz lançamentos.");
    }

    [Fact]
    public async Task LerAsync_Quando_NenhumaLinhaComFinal_Deve_RetornarFalha()
    {
        Stream arquivo = _fixture.ComLinhas(_fixture.Linhas().Where((_, posicao) => posicao + 1 is LinhaColunas or LinhaPagamento));

        Result<Fatura> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("O arquivo não traz o final de nenhum cartão.");
    }

    /// <summary>O arquivo com estes campos trocados numa linha, com as células entre aspas, como o Mago as escreve.</summary>
    /// <param name="numero">O número da linha no arquivo, a partir de 1.</param>
    private static Stream ComCampos(int numero, params (string Coluna, string Valor)[] campos)
    {
        string[] celulas = _fixture.Linhas()[numero - 1].Trim('"').Split("\";\"");
        foreach ((string coluna, string valor) in campos)
            celulas[Array.IndexOf(_colunas, coluna)] = valor.Replace("\"", "\"\"");

        return _fixture.ComLinha(numero, $"\"{string.Join("\";\"", celulas)}\"");
    }

    private async Task<Fatura> LerFixtureAsync()
    {
        await using FileStream arquivo = File.OpenRead(_fixture.Caminho);
        Result<Fatura> result = await _leitor.LerAsync(arquivo, CancellationToken.None);
        return result.Should().Succeed().Which;
    }
}
