using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace ControleFinanceiro.Analyzers;

/// <summary>CF0002: ternário com um ramo que passa de uma linha.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MultilineConditionalBranchAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor Regra = new(
        id: "CF0002",
        title: "Ternário com ramo multilinha",
        messageFormat: "Um ramo do ternário passa de uma linha: use if",
        category: Catalogo.Categoria,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: Catalogo.Url + "#cf0002");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Regra];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analisar, SyntaxKind.ConditionalExpression);
    }

    private static void Analisar(SyntaxNodeAnalysisContext context)
    {
        ConditionalExpressionSyntax ternario = (ConditionalExpressionSyntax)context.Node;
        if (Linhas.OcupaMaisDeUma(ternario.WhenTrue) || Linhas.OcupaMaisDeUma(ternario.WhenFalse))
            context.ReportDiagnostic(Diagnostic.Create(Regra, ternario.QuestionToken.GetLocation()));
    }
}
