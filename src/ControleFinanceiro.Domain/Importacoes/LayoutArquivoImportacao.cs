using Ardalis.SmartEnum;

namespace ControleFinanceiro.Domain.Importacoes;

/// <summary>
/// O layout de arquivo que a importação aceita, um por banco e tipo de origem. O código é o que o usuário
/// informa ao importar e o que fica gravado na importação.
/// </summary>
public sealed class LayoutArquivoImportacao : SmartEnum<LayoutArquivoImportacao, string>
{
    public static readonly LayoutArquivoImportacao C6ContaCsv = new("c6-conta-csv", "Extrato CSV da conta do C6");
    public static readonly LayoutArquivoImportacao C6CartaoCsv = new("c6-cartao-csv", "Fatura CSV do cartão do C6");

    /// <summary>Os layouts de extrato de conta, que a importação do extrato aceita.</summary>
    public static readonly IReadOnlyCollection<LayoutArquivoImportacao> Extratos = [C6ContaCsv];

    /// <summary>Os layouts de fatura de cartão, que a importação da fatura aceita.</summary>
    public static readonly IReadOnlyCollection<LayoutArquivoImportacao> Faturas = [C6CartaoCsv];

    private LayoutArquivoImportacao(string codigo, string name) : base(name, codigo)
    {
    }
}
