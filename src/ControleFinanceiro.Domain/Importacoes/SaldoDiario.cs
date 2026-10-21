using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Importacoes;

/// <summary>
/// O saldo da conta no fim de um dia contábil, como o extrato informa.
/// </summary>
public sealed class SaldoDiario : ValueObject
{
    public DateOnly DataContabil { get; }
    public Reais Saldo { get; }

    private SaldoDiario(DateOnly dataContabil, Reais saldo)
    {
        DataContabil = dataContabil;
        Saldo = saldo;
    }

    public static SaldoDiario Criar(DateOnly dataContabil, Reais saldo) => new(dataContabil, saldo);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return DataContabil;
        yield return Saldo;
    }
}
