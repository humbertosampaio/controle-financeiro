using CsvHelper;

namespace ControleFinanceiro.Infrastructure.Importacao;

/// <summary>
/// As opções de leitura comuns aos arquivos CSV: linha fora do layout é falha, nunca um valor recuperado pelo CsvHelper,
/// e as mensagens das exceções não trazem os valores do arquivo.
/// </summary>
internal static class OpcoesLeituraCsv
{
    public static CsvReaderOptions Criar(char separador)
    {
        return new CsvReaderOptions
        {
            Separator = separador,
            DetectSeparatorHint = false,
            FieldCountMismatch = CsvFieldCountHandling.Throw,
            MalformedData = CsvMalformedDataHandling.Throw,
            MissingField = CsvMissingFieldHandling.Throw,
            IncludeValuesInExceptions = false,
            TrackLineNumbers = true,
        };
    }
}
