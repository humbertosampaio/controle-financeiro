namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarExtrato;

/// <summary>Formulário multipart com os campos <c>layout</c> e <c>arquivo</c>.</summary>
public sealed record class ImportarExtratoRequest
{
    /// <summary>Vem da rota.</summary>
    public Guid IdUsuario { get; init; }

    /// <summary>Código do layout do arquivo (<c>"c6-conta-csv"</c>).</summary>
    public string Layout { get; init; } = string.Empty;

    public IFormFile? Arquivo { get; init; }
}
