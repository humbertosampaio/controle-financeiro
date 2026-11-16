using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace ControleFinanceiro.Analyzers;

/// <summary>CF0009: método de teste do xUnit (<c>[Fact]</c>, <c>[Theory]</c>) fora de <c>Metodo_Quando_Cenario_Deve_Resultado</c>.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TestNameAnalyzer : DiagnosticAnalyzer
{
    private const string AtributoTeste = "Xunit.FactAttribute";

    private static readonly Regex _formato = new(
        "^[A-Z][A-Za-z0-9]*(_Quando_[A-Za-z0-9]+)?_Deve_[A-Za-z0-9]+$",
        RegexOptions.CultureInvariant);

    public static readonly DiagnosticDescriptor Regra = new(
        id: "CF0009",
        title: "Nome de teste fora do formato",
        messageFormat: "O nome '{0}' foge de Metodo_Quando_Cenario_Deve_Resultado",
        category: Catalogo.Categoria,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: Catalogo.Url + "#cf0009");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Regra];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(Analisar, SymbolKind.Method);
    }

    private static void Analisar(SymbolAnalysisContext context)
    {
        IMethodSymbol metodo = (IMethodSymbol)context.Symbol;
        if (metodo.GetAttributes().Any(atributo => EhTeste(atributo.AttributeClass)) && !_formato.IsMatch(metodo.Name))
            context.ReportDiagnostic(Diagnostic.Create(Regra, metodo.Locations[0], metodo.Name));
    }

    /// <summary>O <c>[Theory]</c> e os atributos de teste próprios derivam do <c>[Fact]</c>.</summary>
    private static bool EhTeste(INamedTypeSymbol? atributo)
    {
        for (INamedTypeSymbol? tipo = atributo; tipo is not null; tipo = tipo.BaseType)
        {
            if (tipo.ToDisplayString() == AtributoTeste)
                return true;
        }

        return false;
    }
}
