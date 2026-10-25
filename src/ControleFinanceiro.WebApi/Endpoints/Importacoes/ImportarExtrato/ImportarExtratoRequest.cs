namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarExtrato;

/// <summary>
/// Formulário multipart com os campos <c>arquivo</c> e <c>sufixoConta</c>. O sufixo só vale para o layout cujo arquivo
/// não traz a conta, como o do Mercado Pago.
/// </summary>
public sealed record class ImportarExtratoRequest
{
    /// <summary>Vem da rota.</summary>
    public Guid IdUsuario { get; init; }

    /// <summary>Vem da rota: o código do layout do arquivo (<c>"c6-conta-csv"</c>).</summary>
    public string Layout { get; init; } = string.Empty;

    public IFormFile? Arquivo { get; init; }

    /// <summary>
    /// Os 4 últimos caracteres do número da conta, com o dígito verificador. Obrigatório só nos layouts de
    /// <see cref="Domain.Importacoes.LayoutArquivoImportacao.ExtratosContaInformada"/>.
    /// </summary>
    public string? SufixoConta { get; init; }
}
