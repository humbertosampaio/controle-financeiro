namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarFatura;

/// <summary>Formulário multipart com o campo <c>arquivo</c>.</summary>
public sealed record class ImportarFaturaRequest
{
    /// <summary>Vem da rota.</summary>
    public Guid IdUsuario { get; init; }

    /// <summary>Vem da rota: o código do layout do arquivo (<c>"c6-cartao-csv"</c>).</summary>
    public string Layout { get; init; } = string.Empty;

    public IFormFile? Arquivo { get; init; }
}
