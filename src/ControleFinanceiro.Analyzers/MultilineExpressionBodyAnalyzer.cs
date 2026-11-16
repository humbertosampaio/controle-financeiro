using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace ControleFinanceiro.Analyzers;

/// <summary>CF0001: método, operador, conversão ou função local com <c>=&gt;</c> cuja expressão passa de uma linha.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MultilineExpressionBodyAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor Regra = new(
        id: "CF0001",
        title: "Block body em método multilinha",
        messageFormat: "A expressão do corpo passa de uma linha: use block body",
        category: Catalogo.Categoria,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: Catalogo.Url + "#cf0001");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Regra];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            Analisar,
            SyntaxKind.MethodDeclaration,
            SyntaxKind.OperatorDeclaration,
            SyntaxKind.ConversionOperatorDeclaration,
            SyntaxKind.LocalFunctionStatement);
    }

    private static void Analisar(SyntaxNodeAnalysisContext context)
    {
        ArrowExpressionClauseSyntax? corpo = context.Node switch
        {
            BaseMethodDeclarationSyntax metodo => metodo.ExpressionBody,
            LocalFunctionStatementSyntax funcao => funcao.ExpressionBody,
            _ => null
        };
        if (corpo is not null && Linhas.OcupaMaisDeUma(corpo.Expression))
            context.ReportDiagnostic(Diagnostic.Create(Regra, corpo.ArrowToken.GetLocation()));
    }
}
