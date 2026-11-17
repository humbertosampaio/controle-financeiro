using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace ControleFinanceiro.Analyzers;

/// <summary>
/// CF0011: literal passado sem o nome do parâmetro numa chamada de mais de um argumento. Conta como literal o
/// <c>null</c>, o booleano, o número (com sinal ou não) e a coleção vazia <c>[]</c>. Ficam de fora a chamada de um
/// argumento só, os construtores de data e hora, os <c>Throw*</c> das <c>Argument*Exception</c>, a lista de um
/// <c>params</c>, o <c>TheoryData.Add</c>, a criação de um SmartEnum, os atributos, a cadeia que passa por
/// <c>Should()</c> e a chamada de um delegate, cujos parâmetros não têm nome que diga o papel.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NamedLiteralArgumentAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> _tiposData = ImmutableHashSet.Create(
        "System.DateOnly",
        "System.DateTime",
        "System.DateTimeOffset",
        "System.TimeOnly");

    public static readonly DiagnosticDescriptor Regra = new(
        id: "CF0011",
        title: "Argumento literal sem nome",
        messageFormat: "Passe o literal '{0}' com o nome do parâmetro: '{1}: {0}'",
        category: Catalogo.Categoria,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: Catalogo.Url + "#cf0011");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Regra];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analisar, SyntaxKind.ArgumentList);
    }

    private static void Analisar(SyntaxNodeAnalysisContext context)
    {
        ArgumentListSyntax lista = (ArgumentListSyntax)context.Node;
        if (lista.Arguments.Count < 2 || !lista.Arguments.Any(argumento => argumento.NameColon is null && EhLiteral(argumento.Expression)))
            return;

        if (context.SemanticModel.GetSymbolInfo(lista.Parent!, context.CancellationToken).Symbol is not IMethodSymbol metodo
            || FicaDeFora(metodo)
            || PassaPorShould(lista.Parent))
            return;

        for (int indice = 0; indice < lista.Arguments.Count; indice++)
        {
            ArgumentSyntax argumento = lista.Arguments[indice];
            if (argumento.NameColon is not null || !EhLiteral(argumento.Expression))
                continue;

            IParameterSymbol? parametro = Parametro(metodo, indice);
            if (parametro is not null && !parametro.IsParams)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Regra,
                    argumento.GetLocation(),
                    argumento.Expression.ToString(),
                    parametro.Name));
            }
        }
    }

    private static bool EhLiteral(ExpressionSyntax expressao)
    {
        return expressao switch
        {
            LiteralExpressionSyntax literal => literal.Kind() is SyntaxKind.NullLiteralExpression
                or SyntaxKind.TrueLiteralExpression
                or SyntaxKind.FalseLiteralExpression
                or SyntaxKind.NumericLiteralExpression,
            PrefixUnaryExpressionSyntax sinal => sinal.Operand.IsKind(SyntaxKind.NumericLiteralExpression),
            CollectionExpressionSyntax colecao => colecao.Elements.Count == 0,
            _ => false,
        };
    }

    /// <summary>O argumento depois do último parâmetro vai para o <c>params</c>.</summary>
    private static IParameterSymbol? Parametro(IMethodSymbol metodo, int indice)
    {
        if (metodo.Parameters.Length == 0)
            return null;

        return metodo.Parameters[Math.Min(indice, metodo.Parameters.Length - 1)];
    }

    private static bool FicaDeFora(IMethodSymbol metodo)
    {
        INamedTypeSymbol tipo = metodo.ContainingType;
        return metodo.MethodKind == MethodKind.DelegateInvoke
            || (metodo.MethodKind == MethodKind.Constructor && _tiposData.Contains(tipo.ToDisplayString()))
            || (metodo.Name.StartsWith("Throw", StringComparison.Ordinal) && EhArgumentException(tipo))
            || (metodo.Name == "Add" && EhTheoryData(tipo))
            || (metodo.MethodKind == MethodKind.Constructor && EhSmartEnum(tipo));
    }

    private static bool EhArgumentException(INamedTypeSymbol tipo)
    {
        return tipo.ContainingNamespace.ToDisplayString() == "System"
            && tipo.Name.StartsWith("Argument", StringComparison.Ordinal)
            && tipo.Name.EndsWith("Exception", StringComparison.Ordinal);
    }

    private static bool EhTheoryData(INamedTypeSymbol tipo)
    {
        for (INamedTypeSymbol? atual = tipo; atual is not null; atual = atual.BaseType)
        {
            if (atual.ContainingNamespace.ToDisplayString() == "Xunit" && atual.Name == "TheoryData")
                return true;
        }

        return false;
    }

    private static bool EhSmartEnum(INamedTypeSymbol tipo)
    {
        for (INamedTypeSymbol? atual = tipo; atual is not null; atual = atual.BaseType)
        {
            if (atual.ContainingNamespace.ToDisplayString() == "Ardalis.SmartEnum" && atual.Name == "SmartEnum")
                return true;
        }

        return false;
    }

    /// <summary>A asserção do FluentAssertions: a chamada pendurada numa cadeia que passa por <c>Should()</c>.</summary>
    private static bool PassaPorShould(SyntaxNode? chamada)
    {
        ExpressionSyntax? atual = (chamada as InvocationExpressionSyntax)?.Expression;
        while (atual is not null)
        {
            switch (atual)
            {
                case MemberAccessExpressionSyntax acesso:
                    atual = acesso.Expression;
                    break;
                case InvocationExpressionSyntax invocacao:
                    if (invocacao.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Should" })
                        return true;

                    atual = invocacao.Expression;
                    break;
                default:
                    return false;
            }
        }

        return false;
    }
}
