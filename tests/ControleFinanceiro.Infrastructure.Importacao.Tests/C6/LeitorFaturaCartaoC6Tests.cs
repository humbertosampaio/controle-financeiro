using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Infrastructure.Importacao.C6;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Infrastructure.Importacao.Tests.C6;

/// <summary>
/// Lê a fatura fictícia em <c>Fixtures/C6/fatura-cartao.csv</c>, no layout do C6. Os casos de erro trocam
/// uma linha dela: o número da linha no teste é o do arquivo, contando a partir de 1.
/// </summary>
public sealed class LeitorFaturaCartaoC6Tests
{
    private const int LinhaColunas = 1;
    private const string TitularFicticio = "FULANA DE TAL FICTICIA";

    /// <summary>O arquivo como o banco exporta: UTF-8 sem BOM e CRLF.</summary>
    private static readonly ArquivoFixture _fixture = new("C6", "fatura-cartao.csv", fimLinha: "\r\n", comBom: false);
    private static readonly string _caminhoExtrato = Path.Combine(AppContext.BaseDirectory, "Fixtures", "C6", "extrato-conta.csv");

    private readonly LeitorFaturaCartaoC6 _leitor = new();

    [Fact]
    public async Task LerAsync_Deve_LerOBancoEOsFinaisNaOrdemDaPrimeiraAparicao()
    {
        Fatura fatura = await LerFixtureAsync();

        fatura.Banco.Should().Be(Banco.C6);
        fatura.Sufixos.Should().Equal((SufixoCartao)"1111", (SufixoCartao)"2222");
    }

    [Fact]
    public async Task LerAsync_Deve_LerOsLancamentosNaOrdemDoArquivoPelaDataDeCompra()
    {
        Fatura fatura = await LerFixtureAsync();

        fatura.Lancamentos.Select(l => l.Lancamento.Data).Should().Equal(
            new DateOnly(2026, 3, 5),
            new DateOnly(2026, 2, 28),
            new DateOnly(2025, 7, 15),
            new DateOnly(2026, 3, 10),
            new DateOnly(2026, 3, 2),
            new DateOnly(2026, 3, 8),
            new DateOnly(2026, 3, 6),
            new DateOnly(2026, 3, 6));
    }

    [Fact]
    public async Task LerAsync_Deve_InverterOSinalParaACompraSairNegativaEOPagamentoEOEstornoPositivos()
    {
        Fatura fatura = await LerFixtureAsync();

        fatura.Lancamentos.Select(l => l.Lancamento.Valor.Value).Should().Equal(
            -123.45m, -18.90m, -99.90m, -55.00m, 500.00m, 35.00m, -12.00m, -12.00m);
    }

    [Fact]
    public async Task LerAsync_Deve_SepararOsLancamentosPeloFinalDoCartao()
    {
        Fatura fatura = await LerFixtureAsync();

        SufixoCartao fisico = (SufixoCartao)"1111";
        SufixoCartao @virtual = (SufixoCartao)"2222";

        fatura.Lancamentos.Select(l => l.SufixoCartao).Should().Equal(
            fisico, @virtual, fisico, fisico, fisico, @virtual, @virtual, @virtual);
    }

    [Fact]
    public async Task LerAsync_Quando_CompraParcelada_Deve_LerAParcelaComADataDaCompraOriginal()
    {
        Fatura fatura = await LerFixtureAsync();

        Lancamento parcelada = fatura.Lancamentos[2].Lancamento;
        parcelada.Parcela.Should().Be((Parcela)(3, 10));
        parcelada.Data.Should().Be(new DateOnly(2025, 7, 15));
    }

    [Fact]
    public async Task LerAsync_Quando_ParcelaUnica_Deve_DeixarSemParcela()
    {
        Fatura fatura = await LerFixtureAsync();

        fatura.Lancamentos.Where((_, posicao) => posicao != 2).Should().AllSatisfy(l => l.Lancamento.Parcela.Should().BeNull());
    }

    [Fact]
    public async Task LerAsync_Quando_ParcelaUmDeUm_Deve_DeixarSemParcela()
    {
        Stream arquivo = _fixture.ComLinha(numero: 2, $"05/03/2026;{TitularFicticio};1111;Supermercado;MERCADO FICTICIO;1/1;0;0;123.45");

        Fatura fatura = (await _leitor.LerAsync(arquivo, CancellationToken.None)).Value;

        fatura.Lancamentos[0].Lancamento.Parcela.Should().BeNull();
        fatura.Lancamentos[0].Lancamento.Identidade.Campos[^1].Should().BeEmpty();
    }

    [Fact]
    public async Task LerAsync_Deve_GuardarACategoriaDoBancoENenhumaQuandoEmBranco()
    {
        Fatura fatura = await LerFixtureAsync();

        fatura.Lancamentos.Select(l => l.Lancamento.CategoriaBanco).Should().Equal(
            "Supermercado", "Restaurante", "Eletrônicos", "Serviços", null, "Vestuário", "Transporte", "Transporte");
    }

    [Fact]
    public async Task LerAsync_Deve_ApararADescricaoSemContraparteNemHora()
    {
        Fatura fatura = await LerFixtureAsync();

        Lancamento comEspacos = fatura.Lancamentos[1].Lancamento;
        comEspacos.Descricao.Value.Should().Be("PADARIA EXEMPLO");
        fatura.Lancamentos.Should().AllSatisfy(l =>
        {
            l.Lancamento.Contraparte.Should().BeNull();
            l.Lancamento.Hora.Should().BeNull();
        });
    }

    [Fact]
    public async Task LerAsync_Quando_CompraInternacional_Deve_LerOValorEmReais()
    {
        Fatura fatura = await LerFixtureAsync();

        fatura.Lancamentos[3].Lancamento.Valor.Value.Should().Be(-55.00m);
    }

    [Fact]
    public async Task LerAsync_Deve_TravarAIdentidadeNaDataNoValorNaDescricaoCruaENaParcela()
    {
        Fatura fatura = await LerFixtureAsync();

        fatura.Lancamentos[0].Lancamento.Identidade.Campos.Should().Equal("2026-03-05", "-123.45", "MERCADO FICTICIO", "");
        fatura.Lancamentos[2].Lancamento.Identidade.Campos.Should().Equal("2025-07-15", "-99.90", "LOJA DE EXEMPLO", "3/10");
    }

    [Fact]
    public async Task LerAsync_Quando_DuasLinhasIdenticas_Deve_LerDoisLancamentosComAMesmaIdentidade()
    {
        Fatura fatura = await LerFixtureAsync();

        Lancamento primeira = fatura.Lancamentos[6].Lancamento;
        Lancamento segunda = fatura.Lancamentos[7].Lancamento;
        primeira.Should().NotBeSameAs(segunda);
        primeira.Identidade.Should().Be(segunda.Identidade);
    }

    [Fact]
    public async Task LerAsync_Deve_DeixarONomeDoTitularForaDeTodoLancamento()
    {
        Fatura fatura = await LerFixtureAsync();

        fatura.Lancamentos.Should().AllSatisfy(l =>
        {
            Lancamento lancamento = l.Lancamento;
            string?[] textos = [lancamento.Descricao.Value, lancamento.Contraparte, lancamento.CategoriaBanco, .. lancamento.Identidade.Campos];
            textos.Should().NotContain(t => t != null && t.Contains("FULANA", StringComparison.OrdinalIgnoreCase));
        });
    }

    [Theory]
    [InlineData($"05/03/2026;{TitularFicticio};111;Supermercado;MERCADO FICTICIO;Única;0;0;123.45", "não tem o final do cartão com 4 dígitos.")]
    [InlineData($"05/03/2026;{TitularFicticio};11A1;Supermercado;MERCADO FICTICIO;Única;0;0;123.45", "não tem o final do cartão com 4 dígitos.")]
    [InlineData($"05/03/2026;{TitularFicticio};1111;Supermercado;MERCADO FICTICIO;3 de 10;0;0;123.45", "não tem a parcela no formato do C6.")]
    [InlineData($"05/03/2026;{TitularFicticio};1111;Supermercado;MERCADO FICTICIO;11/10;0;0;123.45", "não tem a parcela no formato do C6.")]
    [InlineData($"05/03/2026;{TitularFicticio};1111;Supermercado;MERCADO FICTICIO;0/10;0;0;123.45", "não tem a parcela no formato do C6.")]
    [InlineData($"05/03/2026;{TitularFicticio};1111;Supermercado;\"  \";Única;0;0;123.45", "não tem descrição.")]
    public async Task LerAsync_Quando_CampoInvalido_Deve_RetornarFalhaSoComONumeroDaLinha(string linha, string erro)
    {
        Stream arquivo = _fixture.ComLinha(numero: 4, linha);

        Result<Fatura> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith($"A linha 4 do arquivo {erro}");
    }

    [Theory]
    [InlineData($"31/02/2026;{TitularFicticio};1111;Supermercado;MERCADO FICTICIO;Única;0;0;123.45")]
    [InlineData($"05/03/2026;{TitularFicticio};1111;Supermercado;MERCADO FICTICIO;Única;0;0;1,234.56")]
    [InlineData($"05/03/2026;{TitularFicticio};1111;Supermercado;MERCADO FICTICIO;Única;0;0")]
    [InlineData($"05/03/2026;{TitularFicticio};1111;Supermercado;MERCADO FICTICIO;Única;0;0;123.45;extra")]
    [InlineData($"05/03/2026;{TitularFicticio};1111;Supermercado;MERCADO\"FICTICIO;Única;0;0;123.45")]
    public async Task LerAsync_Quando_LinhaForaDoLayout_Deve_RetornarFalhaSoComONumeroDaLinha(string linha)
    {
        Stream arquivo = _fixture.ComLinha(numero: 3, linha);

        Result<Fatura> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("A linha 3 do arquivo não está no layout da fatura do cartão do C6.");
    }

    [Fact]
    public async Task LerAsync_Quando_ColunasDiferentes_Deve_RetornarFalhaComALinhaDasColunas()
    {
        Stream arquivo = _fixture.ComLinha(LinhaColunas, "Data;Descrição;Valor");

        Result<Fatura> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("A linha 1 do arquivo não traz as colunas da fatura do cartão do C6.");
    }

    [Fact]
    public async Task LerAsync_Quando_ArquivoDoExtratoDaConta_Deve_RetornarFalhaComALinhaDasColunas()
    {
        await using FileStream arquivo = File.OpenRead(_caminhoExtrato);

        Result<Fatura> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("A linha 1 do arquivo não traz as colunas da fatura do cartão do C6.");
    }

    [Fact]
    public async Task LerAsync_Quando_ArquivoVazio_Deve_RetornarFalha()
    {
        Result<Fatura> result = await _leitor.LerAsync(_fixture.ComLinhas(linhas: []), CancellationToken.None);

        result.Should().FailWith("O arquivo termina antes das colunas da fatura do cartão do C6.");
    }

    [Fact]
    public async Task LerAsync_Quando_SoAsColunas_Deve_RetornarFalha()
    {
        Stream arquivo = _fixture.ComLinhas(_fixture.Linhas().Take(LinhaColunas));

        Result<Fatura> result = await _leitor.LerAsync(arquivo, CancellationToken.None);

        result.Should().FailWith("A fatura não traz lançamentos.");
    }

    private async Task<Fatura> LerFixtureAsync()
    {
        await using FileStream arquivo = File.OpenRead(_fixture.Caminho);
        Result<Fatura> result = await _leitor.LerAsync(arquivo, CancellationToken.None);
        return result.Should().Succeed().Which;
    }
}
