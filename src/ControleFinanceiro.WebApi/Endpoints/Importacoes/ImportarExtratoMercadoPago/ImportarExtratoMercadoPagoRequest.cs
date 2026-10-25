namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarExtratoMercadoPago;

/// <summary>
/// Formulário multipart com os campos <c>arquivo</c> e <c>sufixoConta</c>. O arquivo do Mercado Pago não traz a
/// conta, então o sufixo vem do usuário.
/// </summary>
public sealed record class ImportarExtratoMercadoPagoRequest
{
    /// <summary>Vem da rota.</summary>
    public Guid IdUsuario { get; init; }

    public IFormFile? Arquivo { get; init; }

    /// <summary>Os 4 últimos caracteres do número da conta, com o dígito verificador.</summary>
    public string? SufixoConta { get; init; }
}
