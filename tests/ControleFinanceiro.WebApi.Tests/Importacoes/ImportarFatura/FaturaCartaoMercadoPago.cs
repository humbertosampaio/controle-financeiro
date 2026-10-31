using ControleFinanceiro.Domain.Cartoes;
using System.Text;

namespace ControleFinanceiro.WebApi.Tests.Importacoes.ImportarFatura;

/// <summary>
/// Variações da fatura fictícia do Mercado Pago (<c>Fixtures/MercadoPago/fatura-cartao.csv</c>, do projeto de testes
/// da importação), no mesmo layout: UTF-8 sem BOM e CRLF. O arquivo tem 7 lançamentos no final 1234: a parcela 3/10
/// de uma compra de 15/11/2025, cinco compras à vista (duas iguais em tudo menos no número da transação) e o
/// pagamento da fatura, sem final. Os vendedores trazem o CPF e o CNPJ de exemplo, e os produtos, o CNPJ alfanumérico.
/// </summary>
internal static class FaturaCartaoMercadoPago
{
    private const string ParcelaCompraAntiga = "\"100000000001\";\"1234\";\"3\";\"10\"";

    private static readonly string _caminho = Path.Combine(AppContext.BaseDirectory, "Importacoes", "ImportarFatura", "Fixtures", "fatura-cartao-mercado-pago.csv");

    public static readonly SufixoCartao Sufixo = (SufixoCartao)"1234";
    public const int QuantidadeLancamentos = 7;
    public const decimal SomaLancamentos = 0m;
    public static readonly DateOnly PrimeiraData = new(2025, 11, 15);
    public static readonly DateOnly UltimaData = new(2026, 4, 10);

    /// <summary>Os produtos de exemplo, que não podem ser gravados.</summary>
    public static readonly string[] TextosProibidos = ["12.ABC.345/01DE-35", "Ficticio"];

    /// <summary>O arquivo como está em disco.</summary>
    public static byte[] Completo() => File.ReadAllBytes(_caminho);

    /// <summary>A fatura seguinte da compra parcelada: a mesma compra, com o mesmo número, agora na parcela 4/10.</summary>
    public static byte[] ComParcelaSeguinte() => Trocar(ParcelaCompraAntiga, "\"100000000001\";\"1234\";\"4\";\"10\"");

    /// <summary>O arquivo com as duas compras iguais trazendo também o mesmo número da transação.</summary>
    public static byte[] ComLinhasIguais() => Trocar("\"100000000006\"", "\"100000000005\"");

    /// <summary>O arquivo com a coluna <c>status</c> renomeada, fora do prompt.</summary>
    public static byte[] ComColunaRenomeada() => Trocar("\"status\"", "\"situacao\"");

    private static byte[] Trocar(string trecho, string novo)
    {
        string conteudo = File.ReadAllText(_caminho, Encoding.UTF8).Replace(trecho, novo, StringComparison.Ordinal);
        return Encoding.UTF8.GetBytes(conteudo);
    }
}
