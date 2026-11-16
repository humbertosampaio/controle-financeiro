using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace ControleFinanceiro.Analyzers;

/// <summary>CF0004: método de extensão com <c>this</c> no primeiro parâmetro, em vez do bloco <c>extension</c>.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ThisExtensionParameterAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor Regra = new(
        id: "CF0004",
        title: "Extensão com this",
        messageFormat: "Declare a extensão num bloco extension, e não com this no primeiro parâmetro",
        category: Catalogo.Categoria,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: Catalogo.Url + "#cf0004");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Regra];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analisar, SyntaxKind.Parameter);
    }

    private static void Analisar(SyntaxNodeAnalysisContext context)
    {
        ParameterSyntax parametro = (ParameterSyntax)context.Node;
        foreach (SyntaxToken modificador in parametro.Modifiers)
        {
            if (modificador.IsKind(SyntaxKind.ThisKeyword))
                context.ReportDiagnostic(Diagnostic.Create(Regra, modificador.GetLocation()));
        }
    }
}
