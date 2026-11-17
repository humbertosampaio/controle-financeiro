using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace ControleFinanceiro.Analyzers;

/// <summary>CF0012: padrão <c>{ } x</c> para desembrulhar um nullable, no lugar do tipo (<c>is IdCartao x</c>).</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EmptyPropertyPatternAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor Regra = new(
        id: "CF0012",
        title: "Padrão vazio para desembrulhar nullable",
        messageFormat: "Use o tipo no padrão ('is T {0}'), e não '{{ }} {0}'",
        category: Catalogo.Categoria,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: Catalogo.Url + "#cf0012");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Regra];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analisar, SyntaxKind.RecursivePattern);
    }

    private static void Analisar(SyntaxNodeAnalysisContext context)
    {
        RecursivePatternSyntax padrao = (RecursivePatternSyntax)context.Node;
        if (padrao.Type is null
            && padrao.PositionalPatternClause is null
            && padrao.PropertyPatternClause is { Subpatterns.Count: 0 }
            && padrao.Designation is SingleVariableDesignationSyntax variavel)
            context.ReportDiagnostic(Diagnostic.Create(Regra, padrao.GetLocation(), variavel.Identifier.ValueText));
    }
}
