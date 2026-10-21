using System.Text;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Infrastructure.Importacao.C6;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Infrastructure.Importacao.Tests.C6;

/// <summary>
/// Lê o extrato fictício em <c>Fixtures/C6/extrato-conta.csv</c>, no layout do C6. Os casos de erro trocam
/// uma linha dele: o número da linha no teste é o do arquivo, contando a partir de 1.
/// </summary>
public sealed class LeitorExtratoContaC6Tests
{
    private const int LinhaDasColunas = 9;
    private const int LinhaDoPrimeiroLancamento = 10;

    private static readonly string _caminhoFixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "C6", "extrato-conta.csv");

    private readonly LeitorExtratoContaC6 _leitor = new();

    [Fact]
    public async Task LerAsync_Deve_LerOBancoOTipoEOSufixoDaConta()
    {
        ExtratoLido extrato = await LerFixtureAsync();

        extrato.Banco.Should().Be(Banco.C6);
        extrato.TipoConta.Should().Be(TipoConta.ContaCorrente);
        extrato.SufixoConta.Should().Be((SufixoConta)"4321");
    }

    [Fact]
    public async Task LerAsync_Deve_LerOPeriodoDoCabecalho()
    {
        ExtratoLido extrato = await LerFixtureAsync();

        extrato.Periodo.Should().Be(PeriodoExtrato.Criar(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 9)).Value);
    }

    [Fact]
    public async Task LerAsync_Deve_LerOsLancamentosNaOrdemDoArquivoPelaDataDeLancamento()
    {
        ExtratoLido extrato = await LerFixtureAsync();

        extrato.Lancamentos.Select(l => l.Data).Should().Equal(
            Dia(2), Dia(2), Dia(3), Dia(3), Dia(3), Dia(4), Dia(4), Dia(7), Dia(9), Dia(9));
    }

    [Fact]
    public async Task LerAsync_Deve_LerAEntradaComoPositivaEASaidaComoNegativa()
    {
        ExtratoLido extrato = await LerFixtureAsync();

        extrato.Lancamentos.Select(l => l.Valor.Value).Should().Equal(
            150.00m, -89.90m, -230.45m, -5.00m, -5.00m, 12.34m, 0m, 2000.00m, 20.00m, -10.00m);
    }

    [Fact]
    public async Task LerAsync_Quando_DataContabilDiferenteDaDeLancamento_Deve_GuardarAContabilSoNoLancamentoLido()
    {
        ExtratoLido extrato = await LerFixtureAsync();

        LancamentoLido pixDeSabado = extrato.LancamentosLidos[7];
        pixDeSabado.Lancamento.Data.Should().Be(Dia(7));
        pixDeSabado.DataContabil.Should().Be(Dia(9));
    }

    [Fact]
    public async Task LerAsync_Deve_EntregarUmSaldoPorDiaContabil()
    {
        ExtratoLido extrato = await LerFixtureAsync();

        extrato.Saldos.Should().Equal(
            SaldoDiario.Criar(Dia(2), -12.50m),
            SaldoDiario.Criar(Dia(3), -252.95m),
            SaldoDiario.Criar(Dia(4), -240.61m),
            SaldoDiario.Criar(Dia(9), 1769.39m));
    }

    [Fact]
    public async Task LerAsync_Deve_EntregarSaldosQueFechamComOsLancamentos()
    {
        ExtratoLido extrato = await LerFixtureAsync();

        ConferenciaSaldo.Conferir(extrato).Should().Succeed();
    }

    [Fact]
    public async Task LerAsync_Quando_LinhasDoMesmoDiaContabilComSaldosDiferentes_Deve_EntregarOsDoisSaldos()
    {
        Stream arquivo = FixtureComLinha(14, "03/03/2026,03/03/2026,Tarifa de exemplo,,0.00,5.00,-252.96");

        ExtratoLido extrato = (await _leitor.LerAsync(arquivo, CancellationToken.None)).Value;

        extrato.Saldos.Where(s => s.DataContabil == Dia(3)).Should().HaveCount(2);
    }

    [Fact]
    public async Task LerAsync_Deve_TravarAIdentidadeNasColunasCruasDoArquivo()
    {
        ExtratoLido extrato = await LerFixtureAsync();

        extrato.Lancamentos[0].Identidade.Campos.Should().Equal(
            "2026-03-02", "150.00", "PIX RECEBIDO DE FULANO FICTICIO", "DIVISÃO DO JANTAR");
        extrato.Lancamentos[5].Identidade.Campos.Should().Equal(
            "2026-03-04", "12.34", "RENDIMENTO DE FUNDO", "ABCD11");
    }

    [Fact]
    public async Task LerAsync_Quando_DuasLinhasIdenticas_Deve_LerDoisLancamentosComAMesmaIdentidade()
    {
        ExtratoLido extrato = await LerFixtureAsync();

        LancamentoExtrato primeira = extrato.Lancamentos[3];
        LancamentoExtrato segunda = extrato.Lancamentos[4];
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
        ExtratoLido extrato = await LerFixtureAsync();

        LancamentoExtrato lancamento = extrato.Lancamentos[posicao];
        lancamento.Descricao.Value.Should().Be(descricao);
        lancamento.Contraparte.Should().Be(contraparte);
    }

    [Fact]
    public async Task LerAsync_Quando_DescricaoRepeteOTituloDoPix_Deve_DescartarADescricaoParaONomeNaoVoltar()
    {
        Stream arquivo = FixtureComLinha(
            LinhaDoPrimeiroLancamento,
            "02/03/2026,02/03/2026,Pix recebido de FULANO FICTICIO,PIX RECEBIDO DE FULANO FICTICIO,150.00,0.00,-12.50");

        ExtratoLido extrato = (await _leitor.LerAsync(arquivo, CancellationToken.None)).Value;

        extrato.Lancamentos[0].Descricao.Value.Should().Be("Pix recebido de");
        extrato.Lancamentos[0].Contraparte.Should().Be("FULANO FICTICIO");
    }

    [Fact]
    public async Task LerAsync_Quando_EntradaESaidaPreenchidas_Deve_RetornarFalhaComONumeroDaLinha()
    {
        Stream arquivo = FixtureComLinha(12, "03/03/2026,03/03/2026,Pagamento de boleto,,1.00,230.45,-252.95");

        Result<ExtratoLido> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("A linha 12 do arquivo tem entrada e saída no mesmo lançamento.");
    }

    [Fact]
    public async Task LerAsync_Quando_TituloEmBranco_Deve_RetornarFalhaComONumeroDaLinha()
    {
        Stream arquivo = FixtureComLinha(15, "04/03/2026,04/03/2026,\" \",ABCD11,12.34,0.00,-240.61");

        Result<ExtratoLido> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

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
        Stream arquivo = FixtureComLinha(13, linha);

        Result<ExtratoLido> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("A linha 13 do arquivo não está no layout do extrato da conta do C6.");
    }

    [Fact]
    public async Task LerAsync_Quando_ColunasDiferentes_Deve_RetornarFalhaComALinhaDasColunas()
    {
        Stream arquivo = FixtureComLinha(LinhaDasColunas, "Data,Descrição,Valor");

        Result<ExtratoLido> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("A linha 9 do arquivo não traz as colunas do extrato da conta do C6.");
    }

    [Fact]
    public async Task LerAsync_Quando_ArquivoTerminaAntesDasColunas_Deve_RetornarFalha()
    {
        Stream arquivo = Arquivo(LinhasDaFixture().Take(LinhaDasColunas - 2));

        Result<ExtratoLido> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("O arquivo termina antes das colunas do extrato da conta do C6.");
    }

    [Fact]
    public async Task LerAsync_Quando_CabecalhoSemConta_Deve_RetornarFalha()
    {
        Stream arquivo = FixtureComLinha(2, "Agência: 1");

        Result<ExtratoLido> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("O cabeçalho do arquivo não traz a conta do extrato.");
    }

    [Theory]
    [InlineData("Agência: 1 / Conta: 12345-6", "O número da conta no cabeçalho do arquivo não está no formato do C6.")]
    [InlineData("Agência: 1 / Conta: 123", "O número da conta no cabeçalho do arquivo é curto demais.")]
    public async Task LerAsync_Quando_NumeroDaContaForaDoFormato_Deve_RetornarFalha(string linha, string erro)
    {
        Stream arquivo = FixtureComLinha(2, linha);

        Result<ExtratoLido> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith(erro);
    }

    [Fact]
    public async Task LerAsync_Quando_NumeroDaContaComVerificadorX_Deve_LerOSufixoComX()
    {
        Stream arquivo = FixtureComLinha(2, "Agência: 1 / Conta: 98765432x");

        ExtratoLido extrato = (await _leitor.LerAsync(arquivo, CancellationToken.None)).Value;

        extrato.SufixoConta.Should().Be((SufixoConta)"432X");
    }

    [Theory]
    [InlineData("Extrato de 31/02/2026 a 09/03/2026", "O período no cabeçalho do arquivo não tem datas válidas.")]
    [InlineData("Extrato de 09/03/2026 a 02/03/2026", "O início do período do extrato não pode ser depois do fim.")]
    public async Task LerAsync_Quando_PeriodoInvalido_Deve_RetornarFalha(string linha, string erro)
    {
        Stream arquivo = FixtureComLinha(4, linha);

        Result<ExtratoLido> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith(erro);
    }

    [Fact]
    public async Task LerAsync_Quando_CabecalhoSemPeriodo_Deve_RetornarFalha()
    {
        Stream arquivo = FixtureComLinha(4, "Extrato do mês");

        Result<ExtratoLido> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("O cabeçalho do arquivo não traz o período do extrato.");
    }

    [Fact]
    public async Task LerAsync_Quando_SoAsColunas_Deve_LerOExtratoSemLancamentos()
    {
        Stream arquivo = Arquivo(LinhasDaFixture().Take(LinhaDoPrimeiroLancamento - 1));

        ExtratoLido extrato = (await _leitor.LerAsync(arquivo, CancellationToken.None)).Value;

        extrato.Lancamentos.Should().BeEmpty();
        extrato.Saldos.Should().BeEmpty();
    }

    private async Task<ExtratoLido> LerFixtureAsync()
    {
        await using FileStream arquivo = File.OpenRead(_caminhoFixture);
        Result<ExtratoLido> result = await _leitor.LerAsync(arquivo, CancellationToken.None);
        return result.Should().Succeed().Which;
    }

    private static DateOnly Dia(int dia) => new(2026, 3, dia);

    private static string[] LinhasDaFixture() => File.ReadAllText(_caminhoFixture, Encoding.UTF8).Split("\r\n");

    /// <param name="numero">O número da linha no arquivo, a partir de 1.</param>
    private static Stream FixtureComLinha(int numero, string linha)
    {
        string[] linhas = LinhasDaFixture();
        linhas[numero - 1] = linha;
        return Arquivo(linhas);
    }

    /// <summary>O arquivo como o C6 exporta: UTF-8 com BOM e CRLF.</summary>
    private static Stream Arquivo(IEnumerable<string> linhas)
    {
        byte[] conteudo = Encoding.UTF8.GetBytes(string.Join("\r\n", linhas));
        return new MemoryStream([.. Encoding.UTF8.Preamble, .. conteudo]);
    }
}
