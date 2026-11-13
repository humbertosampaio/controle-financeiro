using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Infrastructure.Importacao.MercadoPago;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Infrastructure.Importacao.Tests.MercadoPago;

/// <summary>
/// Lê o extrato fictício em <c>Fixtures/MercadoPago/extrato-conta.csv</c>, no layout do Mercado Pago. Os casos de
/// erro trocam uma linha dele: o número da linha no teste é o do arquivo, contando a partir de 1.
/// </summary>
public sealed class LeitorExtratoContaMercadoPagoTests
{
    private const int LinhaSaldos = 2;
    private const int LinhaColunas = 4;
    private const int LinhaSegundoLancamento = 6;

    /// <summary>O arquivo como o banco exporta: UTF-8 sem BOM e LF.</summary>
    private static readonly ArquivoFixture _fixture = new("MercadoPago", "extrato-conta.csv", fimLinha: "\n", comBom: false);
    private static readonly SufixoConta _sufixo = (SufixoConta)"1234";

    private readonly LeitorExtratoContaMercadoPago _leitor = new();

    [Fact]
    public async Task LerAsync_Deve_LerOBancoOTipoEOSufixoInformado()
    {
        Extrato extrato = await LerFixtureAsync();

        extrato.Banco.Should().Be(Banco.MercadoPago);
        extrato.TipoConta.Should().Be(TipoConta.ContaPagamento);
        extrato.SufixoConta.Should().Be(_sufixo);
    }

    [Fact]
    public async Task LerAsync_Deve_TerOPeriodoDaPrimeiraAUltimaData()
    {
        Extrato extrato = await LerFixtureAsync();

        extrato.Periodo.Should().Be((PeriodoArquivo)(Dia(2), Dia(9)));
    }

    [Fact]
    public async Task LerAsync_Deve_LerOsValoresComSinalEMilhar()
    {
        Extrato extrato = await LerFixtureAsync();

        extrato.Lancamentos.Select(l => l.Valor).Should().Equal(
            new Reais[] { 0.35m, -120.00m, 2500.00m, -45.90m, -1034.12m, -800.00m, 1000.00m, -10.00m, -9.90m, 0.42m });
    }

    [Fact]
    public async Task LerAsync_Deve_LerOsLancamentosNaOrdemDoArquivoComADataComoContabil()
    {
        Extrato extrato = await LerFixtureAsync();

        extrato.LancamentosContabilizados.Should().AllSatisfy(l => l.DataContabil.Should().Be(l.Lancamento.Data));
        extrato.Lancamentos.Select(l => l.Data).Should().Equal(
            Dia(2), Dia(3), Dia(3), Dia(3), Dia(5), Dia(5), Dia(6), Dia(9), Dia(9), Dia(9));
    }

    [Fact]
    public async Task LerAsync_Deve_IdentificarCadaLancamentoPeloIdDeReferencia()
    {
        Extrato extrato = await LerFixtureAsync();

        extrato.Lancamentos[2].Identidade.Should().Be(IdentidadeLancamento.Unica("1000000000003").Value);
        extrato.Lancamentos.Should().AllSatisfy(l => l.Identidade.EhUnica.Should().BeTrue());
    }

    [Theory]
    [InlineData(0, "Rendimentos", null)]
    [InlineData(1, "Pix enviado", "FULANO FICTICIO")]
    [InlineData(2, "Pix recebido", "BELTRANO FICTICIO 12345678909")]
    [InlineData(3, "Pagamento com QR Pix", "LOJA FICTICIA LTDA")]
    [InlineData(4, "Pagamento de conta", "CONDOMINIO FICTICIO")]
    [InlineData(5, "Débito automático Fatura do cartão", null)]
    [InlineData(6, "Dinheiro retirado Reserva", null)]
    [InlineData(7, "Pix enviado", "CICRANO FICTICIO 123.456.789-09")]
    [InlineData(8, "Pagamento de assinatura", "Clube Ficticio")]
    public async Task LerAsync_Deve_SepararAContraparteDosPrefixosQueATrazem(int posicao, string descricao, string? contraparte)
    {
        Extrato extrato = await LerFixtureAsync();

        Lancamento lancamento = extrato.Lancamentos[posicao];
        lancamento.Descricao.Should().Be((TextoObrigatorio)descricao);
        lancamento.Contraparte.Should().Be(contraparte);
    }

    [Fact]
    public async Task LerAsync_Deve_ManterOCpfDaDescricao()
    {
        Extrato extrato = await LerFixtureAsync();

        extrato.Lancamentos.Should().Contain(l => $"{l.Descricao} {l.Contraparte}".Contains("123.456.789-09"));
    }

    [Fact]
    public async Task LerAsync_Deve_EntregarOSaldoDeCadaDiaComAVesperaVindaDoSaldoInicial()
    {
        Extrato extrato = await LerFixtureAsync();

        extrato.Saldos.Should().Equal(
            SaldoDiario.Criar(new DateOnly(2026, 3, 1), saldo: 1250.00m),
            SaldoDiario.Criar(Dia(2), saldo: 1250.35m),
            SaldoDiario.Criar(Dia(3), saldo: 3584.45m),
            SaldoDiario.Criar(Dia(5), saldo: 1750.33m),
            SaldoDiario.Criar(Dia(6), saldo: 2750.33m),
            SaldoDiario.Criar(Dia(9), saldo: 2730.85m));
    }

    [Fact]
    public async Task LerAsync_Quando_PrimeiroDiaNaoFechaComOSaldoInicial_Deve_RetornarFalhaComODia()
    {
        Stream arquivo = _fixture.ComLinha(LinhaSaldos, "1.250,01;3.500,77;-2.019,92;2.730,85");

        Result<Extrato, FalhaLeituraArquivo> result = await _leitor.LerAsync(arquivo, _sufixo, CancellationToken.None);

        result.Should().FailWith(new FalhaLeituraArquivo(Linha: null, TipoFalhaLeituraArquivo.ConteudoInvalido, "O saldo do dia 02/03/2026 não fecha com os lançamentos do extrato."));
    }

    [Fact]
    public async Task LerAsync_Quando_SaldoFinalNaoFechaComAUltimaTransacao_Deve_RetornarFalha()
    {
        Stream arquivo = _fixture.ComLinha(LinhaSaldos, "1.250,00;3.500,77;-2.019,92;2.730,86");

        Result<Extrato, FalhaLeituraArquivo> result = await _leitor.LerAsync(arquivo, _sufixo, CancellationToken.None);

        result.Should().FailWith(new FalhaLeituraArquivo(Linha: null, TipoFalhaLeituraArquivo.ConteudoInvalido, "O saldo final do arquivo não fecha com o saldo da última transação."));
    }

    [Theory]
    [InlineData("31-02-2026;Pix enviado TEXTO-DO-ARQUIVO;1000000002;-120,00;1.130,35")]
    [InlineData("03/03/2026;Pix enviado TEXTO-DO-ARQUIVO;1000000002;-120,00;1.130,35")]
    [InlineData("03-03-2026;Pix enviado TEXTO-DO-ARQUIVO;1000000002;-120.00;1.130,35")]
    [InlineData("03-03-2026;Pix enviado TEXTO-DO-ARQUIVO;1000000002;-12,0;1.130,35")]
    [InlineData("03-03-2026;Pix enviado TEXTO-DO-ARQUIVO;1000000002;-120,00;11.30,35")]
    [InlineData("03-03-2026;Pix enviado TEXTO-DO-ARQUIVO;1000000002;-١٢٠,00;1.130,35")]
    [InlineData("03-03-2026;Pix enviado TEXTO-DO-ARQUIVO;1000000002;-120,00")]
    [InlineData("03-03-2026;Pix enviado TEXTO-DO-ARQUIVO;1000000002;-120,00;1.130,35;extra")]
    [InlineData("03-03-2026;Pix enviado TEXTO-DO-ARQUIVO;1000000002;-1.000.000.000.000.000,00;1.130,35")]
    public async Task LerAsync_Quando_LinhaForaDoLayout_Deve_RetornarFalhaSoComONumeroDaLinha(string linha)
    {
        Stream arquivo = _fixture.ComLinha(LinhaSegundoLancamento, linha);

        Result<Extrato, FalhaLeituraArquivo> result = await _leitor.LerAsync(arquivo, _sufixo, CancellationToken.None);

        result.Should().FailWith(new FalhaLeituraArquivo(Linha: 6, TipoFalhaLeituraArquivo.ForaDoLayout, "A linha 6 do arquivo não está no layout do extrato da conta do Mercado Pago."));
    }

    [Fact]
    public async Task LerAsync_Quando_DescricaoSoComDocumento_Deve_ManterODocumentoNaDescricao()
    {
        Stream arquivo = _fixture.ComLinha(LinhaSegundoLancamento, "03-03-2026; 123.456.789-09 ;1000000002;-120,00;1.130,35");

        Result<Extrato, FalhaLeituraArquivo> result = await _leitor.LerAsync(arquivo, _sufixo, CancellationToken.None);

        result.Should().Succeed().Which.Lancamentos.Should().Contain(l => l.Descricao == (TextoObrigatorio)"123.456.789-09");
    }

    [Fact]
    public async Task LerAsync_Quando_DescricaoEmBranco_Deve_RetornarFalhaComONumeroDaLinha()
    {
        Stream arquivo = _fixture.ComLinha(LinhaSegundoLancamento, "03-03-2026; ;1000000002;-120,00;1.130,35");

        Result<Extrato, FalhaLeituraArquivo> result = await _leitor.LerAsync(arquivo, _sufixo, CancellationToken.None);

        result.Should().FailWith(new FalhaLeituraArquivo(Linha: 6, TipoFalhaLeituraArquivo.LancamentoInvalido, "A linha 6 do arquivo não tem descrição."));
    }

    [Fact]
    public async Task LerAsync_Quando_IdDeReferenciaEmBranco_Deve_RetornarFalhaComONumeroDaLinha()
    {
        Stream arquivo = _fixture.ComLinha(LinhaSegundoLancamento, "03-03-2026;Pix enviado FULANO FICTICIO; ;-120,00;1.130,35");

        Result<Extrato, FalhaLeituraArquivo> result = await _leitor.LerAsync(arquivo, _sufixo, CancellationToken.None);

        result.Should().FailWith(new FalhaLeituraArquivo(Linha: 6, TipoFalhaLeituraArquivo.LancamentoInvalido, "A linha 6 do arquivo não tem o identificador da transação."));
    }

    [Theory]
    [InlineData("release_date;TRANSACTION_TYPE;REFERENCE_ID;TRANSACTION_NET_AMOUNT;PARTIAL_BALANCE")]
    [InlineData(" RELEASE_DATE;TRANSACTION_TYPE;REFERENCE_ID;TRANSACTION_NET_AMOUNT;PARTIAL_BALANCE")]
    public async Task LerAsync_Quando_ColunaComOutraGrafia_Deve_RetornarFalhaComALinhaColunas(string colunas)
    {
        Stream arquivo = _fixture.ComLinha(LinhaColunas, colunas);

        Result<Extrato, FalhaLeituraArquivo> result = await _leitor.LerAsync(arquivo, _sufixo, CancellationToken.None);

        result.Should().FailWith(new FalhaLeituraArquivo(Linha: 4, TipoFalhaLeituraArquivo.SemColunas, "A linha 4 do arquivo não traz as colunas do extrato da conta do Mercado Pago."));
    }

    [Fact]
    public async Task LerAsync_Quando_LinhaDeSeparadorAntesDasColunas_Deve_RetornarFalhaComALinhaColunas()
    {
        List<string> linhas = _fixture.Linhas().ToList();
        linhas.Insert(index: LinhaColunas - 1, item: "sep=;");

        Result<Extrato, FalhaLeituraArquivo> result = await _leitor.LerAsync(_fixture.ComLinhas(linhas), _sufixo, CancellationToken.None);

        result.Should().FailWith(new FalhaLeituraArquivo(Linha: 4, TipoFalhaLeituraArquivo.SemColunas, "A linha 4 do arquivo não traz as colunas do extrato da conta do Mercado Pago."));
    }

    [Theory]
    [InlineData("02-03-2026;\"TIPO\nFICTICIO\";1000000002;abc;1,00", nameof(TipoFalhaLeituraArquivo.ForaDoLayout), "não está no layout do extrato da conta do Mercado Pago.")]
    [InlineData("02-03-2026;\" \n \";1000000002;1,00;1,00", nameof(TipoFalhaLeituraArquivo.LancamentoInvalido), "não tem descrição.")]
    public async Task LerAsync_Quando_ErroEmRegistroComQuebraDeLinha_Deve_CitarALinhaOndeORegistroComeca(string linha, string tipo, string erro)
    {
        Stream arquivo = _fixture.ComLinha(LinhaSegundoLancamento, linha);

        Result<Extrato, FalhaLeituraArquivo> result = await _leitor.LerAsync(arquivo, _sufixo, CancellationToken.None);

        result.Should().FailWith(new FalhaLeituraArquivo(Linha: 6, TipoFalhaLeituraArquivo.FromName(tipo), $"A linha 6 do arquivo {erro}"));
    }

    [Fact]
    public async Task LerAsync_Quando_ColunasDiferentes_Deve_RetornarFalhaComALinhaColunas()
    {
        Stream arquivo = _fixture.ComLinha(LinhaColunas, "RELEASE_DATE;TRANSACTION_TYPE;TRANSACTION_NET_AMOUNT;PARTIAL_BALANCE");

        Result<Extrato, FalhaLeituraArquivo> result = await _leitor.LerAsync(arquivo, _sufixo, CancellationToken.None);

        result.Should().FailWith(new FalhaLeituraArquivo(Linha: 4, TipoFalhaLeituraArquivo.SemColunas, "A linha 4 do arquivo não traz as colunas do extrato da conta do Mercado Pago."));
    }

    [Theory]
    [InlineData(1, "INITIAL_BALANCE;CREDITS;DEBITS", nameof(TipoFalhaLeituraArquivo.SemColunas), "A linha 1 do arquivo não traz as colunas do resumo do extrato da conta do Mercado Pago.")]
    [InlineData(2, "1.250,00;3.500,77;-2.019,92", nameof(TipoFalhaLeituraArquivo.ForaDoLayout), "A linha 2 do arquivo não está no layout do resumo do extrato da conta do Mercado Pago.")]
    [InlineData(2, "1250.00;3.500,77;-2.019,92;2.730,85", nameof(TipoFalhaLeituraArquivo.ForaDoLayout), "A linha 2 do arquivo não está no layout do resumo do extrato da conta do Mercado Pago.")]
    [InlineData(3, "TEXTO-DO-ARQUIVO", nameof(TipoFalhaLeituraArquivo.ForaDoLayout), "A linha 3 do arquivo deveria estar em branco no extrato da conta do Mercado Pago.")]
    public async Task LerAsync_Quando_ResumoForaDoLayout_Deve_RetornarFalhaSoComONumeroDaLinha(int numero, string linha, string tipo, string erro)
    {
        Stream arquivo = _fixture.ComLinha(numero, linha);

        Result<Extrato, FalhaLeituraArquivo> result = await _leitor.LerAsync(arquivo, _sufixo, CancellationToken.None);

        result.Should().FailWith(new FalhaLeituraArquivo(numero, TipoFalhaLeituraArquivo.FromName(tipo), erro));
    }

    [Fact]
    public async Task LerAsync_Quando_ArquivoTerminaAntesDasColunas_Deve_RetornarFalha()
    {
        Stream arquivo = _fixture.ComLinhas(_fixture.Linhas().Take(LinhaColunas - 2));

        Result<Extrato, FalhaLeituraArquivo> result = await _leitor.LerAsync(arquivo, _sufixo, CancellationToken.None);

        result.Should().FailWith(new FalhaLeituraArquivo(Linha: null, TipoFalhaLeituraArquivo.SemColunas, "O arquivo termina antes das colunas do extrato da conta do Mercado Pago."));
    }

    [Fact]
    public async Task LerAsync_Quando_SoAsColunas_Deve_RetornarFalhaSemTransacoes()
    {
        Stream arquivo = _fixture.ComLinhas(_fixture.Linhas().Take(LinhaColunas));

        Result<Extrato, FalhaLeituraArquivo> result = await _leitor.LerAsync(arquivo, _sufixo, CancellationToken.None);

        result.Should().FailWith(new FalhaLeituraArquivo(Linha: null, TipoFalhaLeituraArquivo.ConteudoInvalido, "O arquivo não traz transações."));
    }

    private async Task<Extrato> LerFixtureAsync()
    {
        await using FileStream arquivo = File.OpenRead(_fixture.Caminho);
        Result<Extrato, FalhaLeituraArquivo> result = await _leitor.LerAsync(arquivo, _sufixo, CancellationToken.None);
        return result.Should().Succeed().Which;
    }

    private static DateOnly Dia(int dia) => new(2026, 3, dia);
}
