using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace ControleFinanceiro.Analyzers;

/// <summary>CF0005: expressão de coleção que só espalha uma sequência (<c>[.. x]</c>), para materializá-la.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SingleSpreadAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor Regra = new(
        id: "CF0005",
        title: "Spread de um elemento só",
        messageFormat: "Use ToArray() ou ToList() para materializar a sequência, e não um spread sozinho",
        category: Catalogo.Categoria,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: Catalogo.Url + "#cf0005");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Regra];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analisar, SyntaxKind.CollectionExpression);
    }

    private static void Analisar(SyntaxNodeAnalysisContext context)
    {
        CollectionExpressionSyntax colecao = (CollectionExpressionSyntax)context.Node;
        if (colecao.Elements.Count == 1 && colecao.Elements[0] is SpreadElementSyntax)
            context.ReportDiagnostic(Diagnostic.Create(Regra, colecao.GetLocation()));
    }
}
