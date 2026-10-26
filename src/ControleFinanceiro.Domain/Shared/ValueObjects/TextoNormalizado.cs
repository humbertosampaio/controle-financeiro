using System.Globalization;
using System.Text;

namespace ControleFinanceiro.Domain.Shared.ValueObjects;

/// <summary>
/// A forma de um texto usada para comparar, e não para exibir: aparado, com os espaços repetidos juntos, sem acentos
/// e em maiúsculas.
/// </summary>
public sealed class TextoNormalizado : SimpleValueObject<string>
{
    private TextoNormalizado(string value) : base(value)
    {
    }

    /// <param name="texto">Nulo vira um texto vazio, assim como o texto feito só de espaços ou de acentos soltos.</param>
    public static TextoNormalizado Criar(string? texto)
    {
        string espacosUnicos = string.Join(' ', (texto ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        string semAcentos = string.Concat(espacosUnicos
            .Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));

        return new TextoNormalizado(semAcentos.ToUpperInvariant().Normalize(NormalizationForm.FormC));
    }

    public static explicit operator TextoNormalizado(string? texto) => Criar(texto);

    public static explicit operator TextoNormalizado(TextoObrigatorio texto) => Criar(texto);
}
