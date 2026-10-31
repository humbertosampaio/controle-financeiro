using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Infrastructure.Importacao.C6;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Infrastructure.Importacao.Tests.C6;

/// <summary>
/// Lê o extrato fictício em <c>Fixtures/C6/extrato-conta.csv</c>, no layout do C6. Os casos de erro trocam
/// uma linha dele: o número da linha no teste é o do arquivo, contando a partir de 1.
/// </summary>
public sealed class LeitorExtratoContaC6Tests
{
    private const int LinhaColunas = 9;
    private const int LinhaPrimeiroLancamento = 10;

    /// <summary>O arquivo como o banco exporta: UTF-8 com BOM e CRLF.</summary>
    private static readonly ArquivoFixture _fixture = new("C6", "extrato-conta.csv", fimLinha: "\r\n", comBom: true);

    private readonly LeitorExtratoContaC6 _leitor = new();

    [Fact]
    public async Task LerAsync_Deve_LerOBancoOTipoEOSufixoDaConta()
    {
        Extrato extrato = await LerFixtureAsync();

        extrato.Banco.Should().Be(Banco.C6);
        extrato.TipoConta.Should().Be(TipoConta.ContaCorrente);
        extrato.SufixoConta.Should().Be((SufixoConta)"4321");
    }

    [Fact]
    public async Task LerAsync_Deve_LerOPeriodoDoCabecalho()
    {
        Extrato extrato = await LerFixtureAsync();

        extrato.Periodo.Should().Be(PeriodoArquivo.Criar(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 9)).Value);
    }

    [Fact]
    public async Task LerAsync_Deve_LerOsLancamentosNaOrdemDoArquivoPelaDataDeLancamento()
    {
        Extrato extrato = await LerFixtureAsync();

        extrato.Lancamentos.Select(l => l.Data).Should().Equal(
            Dia(2), Dia(2), Dia(3), Dia(3), Dia(3), Dia(4), Dia(4), Dia(7), Dia(9), Dia(9));
    }

    [Fact]
    public async Task LerAsync_Deve_LerAEntradaComoPositivaEASaidaComoNegativa()
    {
        Extrato extrato = await LerFixtureAsync();

        extrato.Lancamentos.Select(l => l.Valor.Value).Should().Equal(
            150.00m, -89.90m, -230.45m, -5.00m, -5.00m, 12.34m, 0m, 2000.00m, 20.00m, -10.00m);
    }

    [Fact]
    public async Task LerAsync_Quando_DataContabilDiferenteDaDeLancamento_Deve_GuardarAContabilSoNoLancamentoContabilizado()
    {
        Extrato extrato = await LerFixtureAsync();

        LancamentoContabilizado pixSabado = extrato.LancamentosContabilizados[7];
        pixSabado.Lancamento.Data.Should().Be(Dia(7));
        pixSabado.DataContabil.Should().Be(Dia(9));
    }

    [Fact]
    public async Task LerAsync_Deve_EntregarUmSaldoPorDiaContabil()
    {
        Extrato extrato = await LerFixtureAsync();

        extrato.Saldos.Should().Equal(
            SaldoDiario.Criar(Dia(2), saldo: -12.50m),
            SaldoDiario.Criar(Dia(3), saldo: -252.95m),
            SaldoDiario.Criar(Dia(4), saldo: -240.61m),
            SaldoDiario.Criar(Dia(9), saldo: 1769.39m));
    }

    [Fact]
    public async Task LerAsync_Quando_LinhasDoMesmoDiaContabilComSaldosDiferentes_Deve_RetornarFalhaComODia()
    {
        Stream arquivo = _fixture.ComLinha(numero: 14, "03/03/2026,03/03/2026,Tarifa de exemplo,,0.00,5.00,-252.96");

        Result<Extrato> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("O extrato traz mais de um saldo nestes dias: 03/03/2026.");
    }

    [Fact]
    public async Task LerAsync_Deve_TravarAIdentidadeNasColunasCruasDoArquivo()
    {
        Extrato extrato = await LerFixtureAsync();

        extrato.Lancamentos[0].Identidade.Campos.Should().Equal(
            "2026-03-02", "150.00", "PIX RECEBIDO DE FULANO FICTICIO", "DIVISÃO DO JANTAR");
        extrato.Lancamentos[5].Identidade.Campos.Should().Equal(
            "2026-03-04", "12.34", "RENDIMENTO DE FUNDO", "ABCD11");
    }

    [Fact]
    public async Task LerAsync_Quando_DuasLinhasIdenticas_Deve_LerDoisLancamentosComAMesmaIdentidade()
    {
        Extrato extrato = await LerFixtureAsync();

        Lancamento primeira = extrato.Lancamentos[3];
        Lancamento segunda = extrato.Lancamentos[4];
        primeira.Should().NotBeSameAs(segunda);
        primeira.Identidade.Should().Be(segunda.Identidade);
    }

    [Theory]
    [InlineData(0, "Pix recebido de · Divisão do jantar", "FULANO FICTICIO")]
    [InlineData(1, "Pix enviado para", "LOJA FICTICIA, LTDA")]
    [InlineData(2, "Pagamento de boleto", null)]
    [InlineData(3, "Tarifa de exemplo", null)]
    [InlineData(5, "Rendimento de fundo · ABCD11", null)]
    [InlineData(8, "DEVOL RECEBIDA PIX DE", "LOJA FICTICIA, LTDA")]
    [InlineData(9, "DEVOL ENVIADA PIX", null)]
    public async Task LerAsync_Deve_TirarDaDescricaoONomeDoPixEPorNaContraparte(int posicao, string descricao, string? contraparte)
    {
        Extrato extrato = await LerFixtureAsync();

        Lancamento lancamento = extrato.Lancamentos[posicao];
        lancamento.Descricao.Value.Should().Be(descricao);
        lancamento.Contraparte.Should().Be(contraparte);
    }

    [Fact]
    public async Task LerAsync_Quando_DescricaoRepeteOTituloDoPix_Deve_DescartarADescricaoParaONomeNaoVoltar()
    {
        Stream arquivo = _fixture.ComLinha(
            LinhaPrimeiroLancamento,
            "02/03/2026,02/03/2026,Pix recebido de FULANO FICTICIO,PIX RECEBIDO DE FULANO FICTICIO,150.00,0.00,-12.50");

        Extrato extrato = (await _leitor.LerAsync(arquivo, CancellationToken.None)).Value;

        extrato.Lancamentos[0].Descricao.Value.Should().Be("Pix recebido de");
        extrato.Lancamentos[0].Contraparte.Should().Be("FULANO FICTICIO");
    }

    [Fact]
    public async Task LerAsync_Quando_EntradaESaidaPreenchidas_Deve_RetornarFalhaComONumeroDaLinha()
    {
        Stream arquivo = _fixture.ComLinha(numero: 12, "03/03/2026,03/03/2026,Pagamento de boleto,,1.00,230.45,-252.95");

        Result<Extrato> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("A linha 12 do arquivo tem entrada e saída no mesmo lançamento.");
    }

    [Fact]
    public async Task LerAsync_Quando_ContraparteDoPixSoComDocumento_Deve_ManterODocumentoNaContraparte()
    {
        Stream arquivo = _fixture.ComLinha(numero: 15, "04/03/2026,04/03/2026,Pix recebido de 123.456.789-09,,12.34,0.00,-240.61");

        Result<Extrato> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().Succeed().Which.Lancamentos.Should().ContainSingle(l => l.Contraparte == "123.456.789-09");
    }

    [Fact]
    public async Task LerAsync_Quando_TituloEmBranco_Deve_RetornarFalhaComONumeroDaLinha()
    {
        Stream arquivo = _fixture.ComLinha(numero: 15, "04/03/2026,04/03/2026,\" \",ABCD11,12.34,0.00,-240.61");

        Result<Extrato> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("A linha 15 do arquivo não tem título.");
    }

    [Theory]
    [InlineData("31/02/2026,03/03/2026,TEXTO-DO-ARQUIVO,,0.00,5.00,-252.95")]
    [InlineData("03/03/2026,03/03/2026,TEXTO-DO-ARQUIVO,,0.00,-5.00,-252.95")]
    [InlineData("03/03/2026,03/03/2026,TEXTO-DO-ARQUIVO,,0.00,1.234.56,-252.95")]
    [InlineData("03/03/2026,03/03/2026,TEXTO-DO-ARQUIVO,,0.00,5.00")]
    [InlineData("03/03/2026,03/03/2026,TEXTO-DO-ARQUIVO,,0.00,5.00,-252.95,extra")]
    [InlineData("03/03/2026,03/03/2026,TEXTO\"DO-ARQUIVO,,0.00,5.00,-252.95")]
    public async Task LerAsync_Quando_LinhaForaDoLayout_Deve_RetornarFalhaSoComONumeroDaLinha(string linha)
    {
        Stream arquivo = _fixture.ComLinha(numero: 13, linha);

        Result<Extrato> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("A linha 13 do arquivo não está no layout do extrato da conta do C6.");
    }

    [Theory]
    [InlineData("data lançamento,Data Contábil,Título,Descrição,Entrada(R$),Saída(R$),Saldo do Dia(R$)")]
    [InlineData(" Data Lançamento,Data Contábil,Título,Descrição,Entrada(R$),Saída(R$),Saldo do Dia(R$)")]
    public async Task LerAsync_Quando_ColunaComOutraGrafia_Deve_RetornarFalhaComALinhaDasColunas(string colunas)
    {
        Stream arquivo = _fixture.ComLinha(LinhaColunas, colunas);

        Result<Extrato> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("A linha 9 do arquivo não traz as colunas do extrato da conta do C6.");
    }

    [Theory]
    [InlineData("02/03/2026,02/03/2026,TITULO FICTICIO,,-150.00,0.00,-12.50")]
    [InlineData("02/03/2026,02/03/2026,TITULO FICTICIO,,0.00,-150.00,-12.50")]
    public async Task LerAsync_Quando_EntradaOuSaidaComSinal_Deve_RetornarFalhaSoComONumeroDaLinha(string linha)
    {
        Stream arquivo = _fixture.ComLinha(LinhaPrimeiroLancamento, linha);

        Result<Extrato> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("A linha 10 do arquivo não está no layout do extrato da conta do C6.");
    }

    [Fact]
    public async Task LerAsync_Quando_LinhaDeSeparadorAntesDasColunas_Deve_RetornarFalhaComALinhaDasColunas()
    {
        List<string> linhas = _fixture.Linhas().ToList();
        linhas.Insert(index: LinhaColunas - 1, item: "sep=,");

        Result<Extrato> result = await _leitor.LerAsync(_fixture.ComLinhas(linhas), CancellationToken.None);

        result.Should().FailWith("A linha 9 do arquivo não traz as colunas do extrato da conta do C6.");
    }

    [Theory]
    [InlineData("02/03/2026,02/03/2026,\"TITULO\nFICTICIO\",,-150.00,0.00,-12.50", "não está no layout do extrato da conta do C6.")]
    [InlineData("02/03/2026,02/03/2026,\" \n \",,150.00,0.00,-12.50", "não tem título.")]
    public async Task LerAsync_Quando_ErroEmRegistroComQuebraDeLinha_Deve_CitarALinhaOndeORegistroComeca(string linha, string erro)
    {
        Stream arquivo = _fixture.ComLinha(LinhaPrimeiroLancamento, linha);

        Result<Extrato> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith($"A linha 10 do arquivo {erro}");
    }

    [Fact]
    public async Task LerAsync_Quando_ColunasDiferentes_Deve_RetornarFalhaComALinhaDasColunas()
    {
        Stream arquivo = _fixture.ComLinha(LinhaColunas, "Data,Descrição,Valor");

        Result<Extrato> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("A linha 9 do arquivo não traz as colunas do extrato da conta do C6.");
    }

    [Fact]
    public async Task LerAsync_Quando_ArquivoTerminaAntesDasColunas_Deve_RetornarFalha()
    {
        Stream arquivo = _fixture.ComLinhas(_fixture.Linhas().Take(LinhaColunas - 2));

        Result<Extrato> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("O arquivo termina antes das colunas do extrato da conta do C6.");
    }

    [Fact]
    public async Task LerAsync_Quando_CabecalhoSemConta_Deve_RetornarFalha()
    {
        Stream arquivo = _fixture.ComLinha(numero: 2, "Agência: 1");

        Result<Extrato> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("O cabeçalho do arquivo não traz a conta do extrato.");
    }

    [Theory]
    [InlineData("Agência: 1 / Conta: 12345-6", "O número da conta no cabeçalho do arquivo não está no formato do C6.")]
    [InlineData("Agência: 1 / Conta: 123", "O número da conta no cabeçalho do arquivo é curto demais.")]
    public async Task LerAsync_Quando_NumeroDaContaForaDoFormato_Deve_RetornarFalha(string linha, string erro)
    {
        Stream arquivo = _fixture.ComLinha(numero: 2, linha);

        Result<Extrato> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith(erro);
    }

    [Fact]
    public async Task LerAsync_Quando_NumeroDaContaComVerificadorX_Deve_LerOSufixoComX()
    {
        Stream arquivo = _fixture.ComLinha(numero: 2, "Agência: 1 / Conta: 98765432x");

        Extrato extrato = (await _leitor.LerAsync(arquivo, CancellationToken.None)).Value;

        extrato.SufixoConta.Should().Be((SufixoConta)"432X");
    }

    [Theory]
    [InlineData("Extrato de 31/02/2026 a 09/03/2026", "O período no cabeçalho do arquivo não tem datas válidas.")]
    [InlineData("Extrato de 09/03/2026 a 02/03/2026", "O início do período não pode ser depois do fim.")]
    public async Task LerAsync_Quando_PeriodoInvalido_Deve_RetornarFalha(string linha, string erro)
    {
        Stream arquivo = _fixture.ComLinha(numero: 4, linha);

        Result<Extrato> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith(erro);
    }

    [Fact]
    public async Task LerAsync_Quando_CabecalhoSemPeriodo_Deve_RetornarFalha()
    {
        Stream arquivo = _fixture.ComLinha(numero: 4, "Extrato do mês");

        Result<Extrato> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("O cabeçalho do arquivo não traz o período do extrato.");
    }

    [Fact]
    public async Task LerAsync_Quando_SoAsColunas_Deve_LerOExtratoSemLancamentos()
    {
        Stream arquivo = _fixture.ComLinhas(_fixture.Linhas().Take(LinhaPrimeiroLancamento - 1));

        Extrato extrato = (await _leitor.LerAsync(arquivo, CancellationToken.None)).Value;

        extrato.Lancamentos.Should().BeEmpty();
        extrato.Saldos.Should().BeEmpty();
    }

    private async Task<Extrato> LerFixtureAsync()
    {
        await using FileStream arquivo = File.OpenRead(_fixture.Caminho);
        Result<Extrato> result = await _leitor.LerAsync(arquivo, CancellationToken.None);
        return result.Should().Succeed().Which;
    }

    private static DateOnly Dia(int dia) => new(2026, 3, dia);
}
