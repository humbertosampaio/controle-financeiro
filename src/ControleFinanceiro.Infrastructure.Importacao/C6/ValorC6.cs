using CsvHelper;
using System.Globalization;

namespace ControleFinanceiro.Infrastructure.Importacao.C6;

/// <summary>Os valores dos arquivos do C6: com ponto decimal e sem separador de milhar.</summary>
internal static class ValorC6
{
    private static decimal Ler(CsvReader leitor, int indice, NumberStyles estilo)
    {
        return decimal.TryParse(leitor.GetString(indice), estilo, CultureInfo.InvariantCulture, out decimal valor)
            ? valor
            : throw new FormatException("O valor não está no formato do C6.");
    }

    internal sealed class SemSinalConverter : ICsvConverter<decimal>
    {
        public decimal Read(CsvReader reader, int index) => Ler(reader, index, NumberStyles.AllowDecimalPoint);

        public void Write(CsvWriter writer, decimal value) => throw new NotSupportedException();
    }

    internal sealed class ComSinalConverter : ICsvConverter<decimal>
    {
        public decimal Read(CsvReader reader, int index) => Ler(reader, index, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint);

        public void Write(CsvWriter writer, decimal value) => throw new NotSupportedException();
    }
}
