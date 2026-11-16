using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace ControleFinanceiro.Analyzers;

/// <summary>
/// CF0007: chamada que quebra os argumentos fora do mesmo grau. A lista conta como quebrada quando algum argumento
/// começa fora da linha do parêntese de abertura; um argumento que só se estende por várias linhas, como uma lambda,
/// não a quebra. Quebrada, nenhum argumento fica na linha da chamada, e eles vão todos juntos numa linha só ou cada
/// um na sua, na mesma coluna, um nível abaixo da linha da chamada.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SiblingArgumentsAnalyzer : DiagnosticAnalyzer
{
    private const int Nivel = 4;

    public static readonly DiagnosticDescriptor Regra = new(
        id: "CF0007",
        title: "Argumentos irmãos fora do mesmo grau",
        messageFormat: "Ao quebrar os argumentos, ponha-os todos numa linha só ou cada um na sua, na mesma coluna, um nível abaixo da chamada",
        category: Catalogo.Categoria,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: Catalogo.Url + "#cf0007");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Regra];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analisar, SyntaxKind.ArgumentList);
    }

    private static void Analisar(SyntaxNodeAnalysisContext context)
    {
        ArgumentListSyntax lista = (ArgumentListSyntax)context.Node;
        if (lista.Arguments.Count == 0)
            return;

        SourceText texto = lista.SyntaxTree.GetText(context.CancellationToken);
        TextLine linhaChamada = texto.Lines.GetLineFromPosition(lista.OpenParenToken.SpanStart);
        LinePosition[] inicios = lista.Arguments
            .Select(argumento => texto.Lines.GetLinePosition(argumento.SpanStart))
            .ToArray();
        if (inicios.All(inicio => inicio.Line == linhaChamada.LineNumber))
            return;

        bool algumLinhaChamada = inicios.Any(inicio => inicio.Line == linhaChamada.LineNumber);
        bool todosMesmaLinha = inicios.All(inicio => inicio.Line == inicios[0].Line);
        bool cadaUmLinhaPropria = inicios.Select(inicio => inicio.Line).Distinct().Count() == inicios.Length
            && inicios.All(inicio => inicio.Character == inicios[0].Character);
        bool umNivelAbaixo = inicios[0].Character == Indentacao(linhaChamada) + Nivel;
        if (algumLinhaChamada || !(todosMesmaLinha || cadaUmLinhaPropria) || !umNivelAbaixo)
            context.ReportDiagnostic(Diagnostic.Create(Regra, lista.OpenParenToken.GetLocation()));
    }

    private static int Indentacao(TextLine linha)
    {
        string conteudo = linha.ToString();
        return conteudo.Length - conteudo.TrimStart(' ').Length;
    }
}
