using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace ControleFinanceiro.Analyzers;

/// <summary>
/// CF0006: conversão explícita cujo operando já é outra conversão explícita (<c>(X)(Y)z</c>). Vem desligado: o
/// <c>.editorconfig</c> o liga só em <c>src/</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoubleCastAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor Regra = new(
        id: "CF0006",
        title: "Duas conversões explícitas seguidas",
        messageFormat: "Duas conversões explícitas seguidas: falta a conversão direta para o tipo final?",
        category: Catalogo.Categoria,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        helpLinkUri: Catalogo.Url + "#cf0006");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Regra];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analisar, SyntaxKind.CastExpression);
    }

    private static void Analisar(SyntaxNodeAnalysisContext context)
    {
        CastExpressionSyntax conversao = (CastExpressionSyntax)context.Node;
        ExpressionSyntax operando = conversao.Expression;
        while (operando is ParenthesizedExpressionSyntax parenteses)
            operando = parenteses.Expression;
        if (operando is CastExpressionSyntax)
            context.ReportDiagnostic(Diagnostic.Create(Regra, conversao.GetLocation()));
    }
}
