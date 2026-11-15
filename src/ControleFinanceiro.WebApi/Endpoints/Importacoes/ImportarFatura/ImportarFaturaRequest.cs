namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarFatura;

/// <summary>
/// Formulário multipart com os campos <c>arquivo</c> e <c>dataVencimento</c>. O vencimento só vale para o layout cujo
/// arquivo não o traz, como o do C6.
/// </summary>
public sealed record class ImportarFaturaRequest
{
    /// <summary>Vem da rota.</summary>
    public Guid IdUsuario { get; init; }

    /// <summary>Vem da rota: o código do layout do arquivo, como o do <see cref="Domain.Importacoes.LayoutArquivoImportacao.C6CartaoCsv"/>.</summary>
    public string Layout { get; init; } = string.Empty;

    public IFormFile? Arquivo { get; init; }

    /// <summary>
    /// A data de vencimento da fatura, em <c>yyyy-MM-dd</c>. Obrigatória nos layouts de
    /// <see cref="Domain.Importacoes.LayoutArquivoImportacao.FaturasVencimentoInformado"/> e recusada nos outros, que a
    /// trazem no arquivo.
    /// </summary>
    public DateOnly? DataVencimento { get; init; }
}
