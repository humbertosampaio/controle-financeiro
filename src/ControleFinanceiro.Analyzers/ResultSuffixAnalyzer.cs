using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace ControleFinanceiro.Analyzers;

/// <summary>CF0009b: tipo cujo nome termina em <c>Result</c>, que se confunde com o do CSharpFunctionalExtensions.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ResultSuffixAnalyzer : DiagnosticAnalyzer
{
    private const string Sufixo = "Result";

    public static readonly DiagnosticDescriptor Regra = new(
        id: "CF0009b",
        title: "Tipo com sufixo Result",
        messageFormat: "O tipo '{0}' termina em Result, que se confunde com o Result do CSharpFunctionalExtensions: use o sufixo da camada",
        category: Catalogo.Categoria,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: Catalogo.Url + "#cf0009b");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Regra];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(Analisar, SymbolKind.NamedType);
    }

    private static void Analisar(SymbolAnalysisContext context)
    {
        string nome = context.Symbol.Name;
        if (nome.Length > Sufixo.Length && nome.EndsWith(Sufixo, StringComparison.Ordinal))
            context.ReportDiagnostic(Diagnostic.Create(Regra, context.Symbol.Locations[0], nome));
    }
}
