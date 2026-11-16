using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace ControleFinanceiro.Analyzers;

/// <summary>CF0003: classe ou struct com construtor primário. Records ficam de fora.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PrimaryConstructorAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor Regra = new(
        id: "CF0003",
        title: "Construtor primário em classe ou struct",
        messageFormat: "'{0}' tem construtor primário: guarde a dependência num campo private readonly, atribuído num construtor comum",
        category: Catalogo.Categoria,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: Catalogo.Url + "#cf0003");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Regra];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analisar, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
    }

    private static void Analisar(SyntaxNodeAnalysisContext context)
    {
        TypeDeclarationSyntax tipo = (TypeDeclarationSyntax)context.Node;
        if (tipo.ParameterList is ParameterListSyntax parametros)
            context.ReportDiagnostic(Diagnostic.Create(Regra, parametros.GetLocation(), tipo.Identifier.Text));
    }
}
