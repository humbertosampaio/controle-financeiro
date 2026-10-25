using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using System.Text;

namespace ControleFinanceiro.WebApi.Tests.Importacoes.ImportarExtratoMercadoPago;

/// <summary>
/// Variações do extrato fictício do Mercado Pago (<c>Fixtures/MercadoPago/extrato-conta.csv</c>, do projeto de testes
/// da importação), no mesmo layout: UTF-8 sem BOM e LF. O arquivo tem 10 transações de 02/03/2026 a 09/03/2026, do
/// saldo inicial de 1.250,00 ao final de 2.730,85, e um CPF fictício, solto numa linha e formatado em outra.
/// </summary>
internal static class ExtratoContaMercadoPago
{
    private const string LinhaSaldos = "1.250,00;3.500,77;-2.019,92;2.730,85";

    private static readonly string _caminho = Path.Combine(AppContext.BaseDirectory, "Importacoes", "ImportarExtratoMercadoPago", "Fixtures", "extrato-conta.csv");

    public const int QuantidadeLancamentos = 10;
    public const decimal SomaLancamentos = 1480.85m;
    public static readonly SufixoConta SufixoConta = (SufixoConta)"1234";
    public static readonly PeriodoArquivo Periodo = (PeriodoArquivo)(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 9));
    public static readonly string[] CpfFicticio = ["12345678909", "123.456.789-09"];

    /// <summary>O arquivo como está em disco.</summary>
    public static byte[] Completo() => File.ReadAllBytes(_caminho);

    /// <summary>O arquivo com o saldo final um centavo acima do saldo da última transação.</summary>
    public static byte[] ComSaldoFinalQueNaoFecha()
    {
        string conteudo = File.ReadAllText(_caminho, Encoding.UTF8)
            .Replace(LinhaSaldos, "1.250,00;3.500,77;-2.019,92;2.730,86", StringComparison.Ordinal);

        return Encoding.UTF8.GetBytes(conteudo);
    }

    /// <summary>O arquivo com a segunda transação trazendo o mesmo <c>REFERENCE_ID</c> da primeira.</summary>
    public static byte[] ComIdReferenciaRepetido()
    {
        string conteudo = File.ReadAllText(_caminho, Encoding.UTF8)
            .Replace(";1000000002;", ";1000000001;", StringComparison.Ordinal);

        return Encoding.UTF8.GetBytes(conteudo);
    }
}
