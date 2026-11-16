using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace ControleFinanceiro.Analyzers;

/// <summary>
/// CF0013: <c>x.Value</c> de um <c>SimpleValueObject&lt;T&gt;</c> ou de um SmartEnum onde o <c>x</c> sozinho dá o mesmo
/// resultado, pelo operador implícito para o valor. Para saber, o analisador troca o <c>x.Value</c> pelo <c>x</c> e
/// confere se o compilador escolhe o mesmo método ou operador e chega ao mesmo tipo. Na interpolação, que chama o
/// <c>ToString()</c>, só o <c>SimpleValueObject&lt;T&gt;</c> dispensa o <c>.Value</c>: o do SmartEnum devolve o
/// <c>Name</c>. Ficam de fora o <c>x.Value</c> que é o receptor de outro acesso (<c>x.Value.Length</c>) e o que está
/// numa expression tree, porque o EF não traduz a chamada ao operador implícito, e o que está no corpo de um operador
/// de conversão, que é onde a conversão implícita se define.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnnecessaryValueAnalyzer : DiagnosticAnalyzer
{
    private static readonly SyntaxAnnotation _marca = new("CF0013");

    public static readonly DiagnosticDescriptor Regra = new(
        id: "CF0013",
        title: "Value desnecessário",
        messageFormat: "Use '{0}' direto, sem o .Value: o resultado é o mesmo",
        category: Catalogo.Categoria,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: Catalogo.Url + "#cf0013");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Regra];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analisar, SyntaxKind.SimpleMemberAccessExpression);
    }

    private static void Analisar(SyntaxNodeAnalysisContext context)
    {
        MemberAccessExpressionSyntax acesso = (MemberAccessExpressionSyntax)context.Node;
        if (acesso.Name.Identifier.ValueText != "Value"
            || EhReceptor(acesso)
            || acesso.Ancestors().Any(no => no is ConversionOperatorDeclarationSyntax))
            return;

        SemanticModel modelo = context.SemanticModel;
        if (modelo.GetSymbolInfo(acesso, context.CancellationToken).Symbol is not IPropertySymbol propriedade)
            return;

        TipoComValor? tipo = TipoDeclarante(propriedade.ContainingType);
        if (tipo is null || DentroExpressionTree(acesso, modelo, context.CancellationToken))
            return;

        bool dispensavel = acesso.Parent is InterpolationSyntax
            ? tipo == TipoComValor.SimpleValueObject
            : MesmoResultadoSemValue(acesso, modelo, context.CancellationToken);
        if (dispensavel)
            context.ReportDiagnostic(Diagnostic.Create(Regra, acesso.GetLocation(), acesso.Expression.ToString()));
    }

    private enum TipoComValor
    {
        SimpleValueObject,
        SmartEnum,
    }

    private static TipoComValor? TipoDeclarante(INamedTypeSymbol tipo)
    {
        if (tipo.ContainingNamespace.ToDisplayString() == "CSharpFunctionalExtensions" && tipo.Name == "SimpleValueObject")
            return TipoComValor.SimpleValueObject;

        if (tipo.ContainingNamespace.ToDisplayString() == "Ardalis.SmartEnum" && tipo.Name == "SmartEnum")
            return TipoComValor.SmartEnum;

        return null;
    }

    /// <summary>O <c>x.Value</c> que é a origem de outro acesso, como <c>x.Value.Length</c>, precisa do valor.</summary>
    private static bool EhReceptor(MemberAccessExpressionSyntax acesso)
    {
        return acesso.Parent switch
        {
            MemberAccessExpressionSyntax pai => pai.Expression == acesso,
            ConditionalAccessExpressionSyntax pai => pai.Expression == acesso,
            ElementAccessExpressionSyntax pai => pai.Expression == acesso,
            _ => false,
        };
    }

    private static bool DentroExpressionTree(SyntaxNode no, SemanticModel modelo, CancellationToken cancellationToken)
    {
        foreach (AnonymousFunctionExpressionSyntax lambda in no.Ancestors().OfType<AnonymousFunctionExpressionSyntax>())
        {
            ITypeSymbol? destino = modelo.GetTypeInfo(lambda, cancellationToken).ConvertedType;
            if (destino is { Name: "Expression", ContainingNamespace: var espaco } && espaco.ToDisplayString() == "System.Linq.Expressions")
                return true;
        }

        return false;
    }

    /// <summary>
    /// Troca o <c>x.Value</c> pelo <c>x</c> no trecho que o contém e compara as duas versões: o mesmo tipo convertido,
    /// por um operador implícito, e o mesmo método ou operador escolhido para quem o recebe.
    /// </summary>
    private static bool MesmoResultadoSemValue(
        MemberAccessExpressionSyntax acesso,
        SemanticModel modelo,
        CancellationToken cancellationToken)
    {
        ITypeSymbol? convertidoOriginal = modelo.GetTypeInfo(acesso, cancellationToken).ConvertedType;
        if (convertidoOriginal is null)
            return false;

        SyntaxNode? pai = acesso.Parent;
        ISymbol? escolhidoOriginal = pai is null ? null : SimboloEscolhido(pai, modelo, cancellationToken);
        ExpressionSyntax semValue = acesso.Expression
            .WithTriviaFrom(acesso)
            .WithAdditionalAnnotations(_marca);
        if (!TryEspecular(acesso, semValue, modelo, out SemanticModel? especulado, out SyntaxNode? trocado)
            || especulado is null
            || trocado is null)
            return false;

        TypeInfo tipoTrocado = especulado.GetTypeInfo(trocado, cancellationToken);
        Conversion conversao = especulado.GetConversion(trocado, cancellationToken);
        ISymbol? escolhidoTrocado = trocado.Parent is null ? null : SimboloEscolhido(trocado.Parent, especulado, cancellationToken);
        return conversao.IsImplicit
            && conversao.IsUserDefined
            && SymbolEqualityComparer.Default.Equals(tipoTrocado.ConvertedType, convertidoOriginal)
            && SymbolEqualityComparer.Default.Equals(escolhidoTrocado, escolhidoOriginal);
    }

    /// <summary>O método, o construtor ou o operador que recebe o valor; o argumento sobe até a chamada.</summary>
    private static ISymbol? SimboloEscolhido(SyntaxNode pai, SemanticModel modelo, CancellationToken cancellationToken)
    {
        SyntaxNode alvo = pai is ArgumentSyntax { Parent.Parent: { } chamada } ? chamada : pai;
        return modelo.GetSymbolInfo(alvo, cancellationToken).Symbol;
    }

    private static bool TryEspecular(
        MemberAccessExpressionSyntax acesso,
        ExpressionSyntax semValue,
        SemanticModel modelo,
        out SemanticModel? especulado,
        out SyntaxNode? trocado)
    {
        especulado = null;
        trocado = null;
        // O inicializador de um local é especulado com a instrução que o declara; só o de campo, propriedade e
        // parâmetro vai sozinho.
        SyntaxNode? trecho = acesso.Ancestors().FirstOrDefault(no => no is StatementSyntax
            or ArrowExpressionClauseSyntax
            or ConstructorInitializerSyntax
            || (no is EqualsValueClauseSyntax && !no.Ancestors().Any(acima => acima is StatementSyntax)));
        if (trecho is null)
            return false;

        SyntaxNode novo = trecho.ReplaceNode(acesso, semValue);
        int posicao = trecho.SpanStart;
        bool especulou = novo switch
        {
            StatementSyntax instrucao => modelo.TryGetSpeculativeSemanticModel(posicao, instrucao, out especulado),
            ArrowExpressionClauseSyntax seta => modelo.TryGetSpeculativeSemanticModel(posicao, seta, out especulado),
            EqualsValueClauseSyntax inicializador => modelo.TryGetSpeculativeSemanticModel(posicao, inicializador, out especulado),
            ConstructorInitializerSyntax construtor => modelo.TryGetSpeculativeSemanticModel(posicao, construtor, out especulado),
            _ => false,
        };
        if (!especulou || especulado is null)
            return false;

        trocado = novo.GetAnnotatedNodes(_marca).FirstOrDefault();
        return trocado is not null;
    }
}
