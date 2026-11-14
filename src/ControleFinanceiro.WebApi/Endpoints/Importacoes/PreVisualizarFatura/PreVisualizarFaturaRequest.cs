namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.PreVisualizarFatura;

/// <summary>Formulário multipart com o campo <c>arquivo</c>.</summary>
public sealed record class PreVisualizarFaturaRequest
{
    /// <summary>Vem da rota.</summary>
    public Guid IdUsuario { get; init; }

    /// <summary>Vem da rota: o código do layout do arquivo, como o do <see cref="Domain.Importacoes.LayoutArquivoImportacao.C6CartaoCsv"/>.</summary>
    public string Layout { get; init; } = string.Empty;

    public IFormFile? Arquivo { get; init; }
}
