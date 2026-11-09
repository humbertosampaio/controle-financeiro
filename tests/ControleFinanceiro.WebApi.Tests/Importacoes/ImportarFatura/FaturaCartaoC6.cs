using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using System.Text;

namespace ControleFinanceiro.WebApi.Tests.Importacoes.ImportarFatura;

/// <summary>
/// Variações da fatura fictícia do C6 (<c>Fixtures/C6/fatura-cartao.csv</c>, do projeto de testes da importação), no
/// mesmo layout: UTF-8 sem BOM e CRLF. O arquivo tem 8 lançamentos, 4 de cada final (1111, depois 2222), com a
/// parcela 3/10 de uma compra de 15/07/2025 no final 1111 e duas compras idênticas no final 2222.
/// </summary>
internal static class FaturaCartaoC6
{
    private const string ParcelaCompraAntiga = "LOJA DE EXEMPLO;3/10";

    private static readonly string _caminho = Path.Combine(AppContext.BaseDirectory, "Importacoes", "ImportarFatura", "Fixtures", "fatura-cartao.csv");

    public static readonly SufixoCartao SufixoFisico = (SufixoCartao)"1111";
    public static readonly SufixoCartao SufixoVirtual = (SufixoCartao)"2222";
    public const int LancamentosPorCartao = 4;
    public const int QuantidadeLancamentos = 8;
    public const string TitularFicticio = "FULANA DE TAL FICTICIA";
    public static readonly DateOnly PrimeiraCompraFisico = new(2025, 7, 15);
    public static readonly DateOnly UltimaCompraFisico = new(2026, 3, 10);

    /// <summary>
    /// O vencimento das compras à vista de março, com o vencimento no dia 10 e o fechamento no dia 3 que os testes dão aos
    /// cartões. A compra à vista de fevereiro vence em 03/2026.
    /// </summary>
    public static readonly MesReferencia VencimentoComprasDeMarco = (MesReferencia)(04, 2026);

    /// <summary>O arquivo como está em disco.</summary>
    public static byte[] Completo() => File.ReadAllBytes(_caminho);

    /// <summary>A fatura seguinte da compra parcelada: a mesma compra antiga, agora na parcela 4/10.</summary>
    public static byte[] ComParcelaSeguinte()
    {
        string conteudo = File.ReadAllText(_caminho, Encoding.UTF8).Replace(ParcelaCompraAntiga, "LOJA DE EXEMPLO;4/10", StringComparison.Ordinal);
        return Encoding.UTF8.GetBytes(conteudo);
    }

    /// <summary>A fatura com duas compras idênticas parceladas: a linha da parcela 3/10 aparece duas vezes.</summary>
    public static byte[] ComCompraParceladaGemea()
    {
        const string QuebraDeLinha = "\r\n";
        IEnumerable<string> linhas = File.ReadAllText(_caminho, Encoding.UTF8)
            .Split(QuebraDeLinha)
            .SelectMany(linha => linha.Contains(ParcelaCompraAntiga, StringComparison.Ordinal) ? [linha, linha] : new[] { linha });

        return Encoding.UTF8.GetBytes(string.Join(QuebraDeLinha, linhas));
    }
}
