using System.Globalization;
using System.Text;

namespace ControleFinanceiro.WebApi.Tests.Importacoes.ImportarExtrato;

/// <summary>
/// Variações do extrato fictício do C6 (<c>Fixtures/C6/extrato-conta.csv</c>, do projeto de testes da importação),
/// no mesmo layout: UTF-8 com BOM e CRLF. O arquivo tem 10 lançamentos de 02/03/2026 a 09/03/2026, conta com
/// final 4321, duas tarifas idênticas no dia 03/03 e um Pix de sábado (07/03) com data contábil na segunda (09/03).
/// </summary>
internal static class ExtratoContaC6
{
    private const string Fim = "\r\n";
    private const string FormatoData = "dd/MM/yyyy";
    private const int LinhasAntesDosLancamentos = 9;

    private static readonly string _caminho = Path.Combine(AppContext.BaseDirectory, "Importacoes", "ImportarExtrato", "Fixtures", "extrato-conta.csv");
    private static readonly UTF8Encoding _utf8ComBom = new(encoderShouldEmitUTF8Identifier: true);

    public const string SufixoConta = "4321";
    public const int QuantidadeLancamentos = 10;
    public static readonly DateOnly Inicio = new(2026, 3, 2);
    public static readonly DateOnly DiaDasTarifasIdenticas = new(2026, 3, 3);

    /// <summary>O número completo da conta no cabeçalho, que não pode ser gravado.</summary>
    public const string NumeroCompletoConta = "987654321";

    /// <summary>O arquivo como está em disco, já com o BOM e o CRLF.</summary>
    public static byte[] Completo() => File.ReadAllBytes(_caminho);

    /// <summary>
    /// Só os lançamentos com data entre <paramref name="inicio"/> e <paramref name="fim"/>, com o período do
    /// cabeçalho trocado para esses dias.
    /// </summary>
    public static byte[] Recortado(DateOnly inicio, DateOnly fim)
    {
        string[] linhas = File.ReadAllText(_caminho).Split(Fim);
        IEnumerable<string> lancamentos = linhas
            .Skip(LinhasAntesDosLancamentos)
            .Where(linha => linha.Length > 0)
            .Where(linha =>
            {
                DateOnly data = DateOnly.ParseExact(linha[..FormatoData.Length], FormatoData, CultureInfo.InvariantCulture);
                return data >= inicio && data <= fim;
            });

        string periodo = $"Extrato de {inicio.ToString(FormatoData, CultureInfo.InvariantCulture)} a {fim.ToString(FormatoData, CultureInfo.InvariantCulture)}";
        IEnumerable<string> cabecalho = linhas.Take(LinhasAntesDosLancamentos).Select(linha => linha.StartsWith("Extrato de", StringComparison.Ordinal) ? periodo : linha);

        return Montar(string.Join(Fim, cabecalho.Concat(lancamentos)) + Fim);
    }

    /// <summary>
    /// O arquivo completo com a descrição de um Pix trocada: a chave desse lançamento muda, e o valor e os
    /// saldos continuam os mesmos.
    /// </summary>
    public static byte[] ComDescricaoTrocada() => Montar(File.ReadAllText(_caminho).Replace("Divisão do jantar", "Divisão do almoço", StringComparison.Ordinal));

    /// <summary>O arquivo completo com uma das tarifas mais cara, o que faz o saldo do dia 03/03 não fechar.</summary>
    public static byte[] ComSaldoQueNaoFecha()
    {
        string conteudo = File.ReadAllText(_caminho);
        int tarifa = conteudo.IndexOf("Tarifa de exemplo,,0.00,5.00", StringComparison.Ordinal);
        return Montar(string.Concat(conteudo.AsSpan(0, tarifa), "Tarifa de exemplo,,0.00,6.00", conteudo.AsSpan(tarifa + "Tarifa de exemplo,,0.00,5.00".Length)));
    }

    private static byte[] Montar(string conteudo) => [.. _utf8ComBom.GetPreamble(), .. _utf8ComBom.GetBytes(conteudo)];
}
