using ControleFinanceiro.Domain.Shared.ValueObjects;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Beneficiarios;

public sealed class AliasBeneficiario : ComparableValueObject
{
    public TextoObrigatorio TextoExtrato { get; }
    public string? CodigoOrigem { get; }

    private AliasBeneficiario(TextoObrigatorio textoExtrato, string? codigoOrigem)
    {
        TextoExtrato = textoExtrato;
        CodigoOrigem = codigoOrigem;
    }

    public static AliasBeneficiario Criar(TextoObrigatorio textoExtrato, string? codigoOrigem) =>
        new(textoExtrato, codigoOrigem);

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return TextoExtrato.Value.ToUpperInvariant();
        yield return CodigoOrigem ?? string.Empty;
    }
}
