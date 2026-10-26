namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarFatura;

/// <summary>
/// Formulário multipart com os campos <c>arquivo</c>, <c>diaVencimento</c> e <c>vencimentos</c>. Os dois últimos só
/// valem para os cartões que a importação criar.
/// </summary>
public sealed record class ImportarFaturaRequest
{
    /// <summary>Vem da rota.</summary>
    public Guid IdUsuario { get; init; }

    /// <summary>Vem da rota: o código do layout do arquivo (<c>"c6-cartao-csv"</c>).</summary>
    public string Layout { get; init; } = string.Empty;

    public IFormFile? Arquivo { get; init; }

    /// <summary>O dia de vencimento dos cartões novos cujo final não está em <see cref="Vencimentos"/>.</summary>
    public int? DiaVencimento { get; init; }

    /// <summary>O dia de vencimento de cada final de cartão novo, em JSON (<c>{"1234":10}</c>).</summary>
    public Dictionary<string, int>? Vencimentos { get; init; }
}
