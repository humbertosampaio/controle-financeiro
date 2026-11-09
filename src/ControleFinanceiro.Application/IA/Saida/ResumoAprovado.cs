using System.Globalization;

namespace ControleFinanceiro.Application.IA.Saida;

/// <summary>
/// O resumo em uma frase de uma escrita em duas etapas: o usuário o lê no pedido de aprovação da confirmação, e ele volta
/// ao servidor nos argumentos dela.
/// </summary>
internal static class ResumoAprovado
{
    /// <summary>
    /// O texto com espaços comuns e sem espaços nas pontas. O <see cref="Domain.Shared.ValueObjects.Reais"/> formata com
    /// espaço não separável, e o cliente pode trocar um espaço por outro.
    /// </summary>
    public static string Normalizar(string texto)
    {
        string comEspacosComuns = string.Concat(texto.Select(c => char.GetUnicodeCategory(c) == UnicodeCategory.SpaceSeparator ? ' ' : c));
        return comEspacosComuns.Trim();
    }

    /// <summary>
    /// Se o resumo que o usuário aprovou é o calculado. Trocar um espaço por outro, ou acrescentar espaços nas pontas, não
    /// muda o que o usuário leu.
    /// </summary>
    /// <param name="calculado">O resumo recalculado, já normalizado; nulo quando a escrita falhou.</param>
    public static bool Confere(string? calculado, string aprovado) =>
        calculado is not null && string.Equals(Normalizar(aprovado), calculado, StringComparison.Ordinal);
}
