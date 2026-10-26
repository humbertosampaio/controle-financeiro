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
    public static readonly LayoutArquivoImportacao MercadoPagoContaCsv = new("mercado-pago-conta-csv", "Extrato CSV da conta do Mercado Pago");
    public static readonly LayoutArquivoImportacao MercadoPagoCartaoCsv = new("mercado-pago-cartao-csv", "Fatura CSV do cartão do Mercado Pago");

    /// <summary>Os layouts de extrato de conta que trazem a conta no arquivo.</summary>
    public static readonly IReadOnlyCollection<LayoutArquivoImportacao> Extratos = [C6ContaCsv];

    /// <summary>Os layouts de extrato de conta sem a conta no arquivo: o usuário informa o sufixo ao importar.</summary>
    public static readonly IReadOnlyCollection<LayoutArquivoImportacao> ExtratosContaInformada = [MercadoPagoContaCsv];

    /// <summary>Os layouts de fatura de cartão, que a importação da fatura aceita.</summary>
    public static readonly IReadOnlyCollection<LayoutArquivoImportacao> Faturas = [C6CartaoCsv, MercadoPagoCartaoCsv];

    private LayoutArquivoImportacao(string codigo, string name) : base(name, codigo)
    {
    }
}
