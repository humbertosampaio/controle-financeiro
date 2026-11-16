using Microsoft.CodeAnalysis;

namespace ControleFinanceiro.Analyzers;

internal static class Linhas
{
    /// <summary>Se o nó, sem a trivia em volta, começa e termina em linhas diferentes.</summary>
    public static bool OcupaMaisDeUma(SyntaxNode no)
    {
        FileLinePositionSpan posicao = no.GetLocation().GetLineSpan();
        return posicao.StartLinePosition.Line != posicao.EndLinePosition.Line;
    }
}
