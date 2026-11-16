using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace ControleFinanceiro.Analyzers.Tests;

/// <summary>
/// Compila um trecho de código em memória e devolve o que o analisador aponta nele. O trecho tem de compilar: com um
/// erro de compilação, o analisador olharia para uma árvore que o código real nunca teria.
/// </summary>
internal static class Analise
{
    private static readonly MetadataReference[] _referencias = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Select(caminho => MetadataReference.CreateFromFile(caminho))
        .ToArray();

    public static async Task<Diagnostic[]> ApontarAsync(DiagnosticAnalyzer analisador, string codigo)
    {
        SyntaxTree arvore = CSharpSyntaxTree.ParseText(
            codigo,
            new CSharpParseOptions(LanguageVersion.Latest),
            path: "Codigo.cs");
        // Liga também o diagnóstico que vem desligado por padrão, como o CF0006, que o .editorconfig liga só em src.
        ImmutableDictionary<string, ReportDiagnostic> ligados = analisador.SupportedDiagnostics
            .ToImmutableDictionary(regra => regra.Id, _ => ReportDiagnostic.Warn);
        CSharpCompilation compilacao = CSharpCompilation.Create(
            "Analise",
            [arvore],
            _referencias,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable,
                specificDiagnosticOptions: ligados));

        compilacao.GetDiagnostics()
            .Where(diagnostico => diagnostico.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();

        ImmutableArray<Diagnostic> apontados = await compilacao
            .WithAnalyzers([analisador])
            .GetAnalyzerDiagnosticsAsync();
        return apontados.ToArray();
    }
}
