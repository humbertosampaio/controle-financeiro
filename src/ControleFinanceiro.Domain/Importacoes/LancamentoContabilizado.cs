using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Domain.Importacoes;

/// <summary>
/// Um lançamento do extrato com o dia contábil em que ele entrou no saldo. A data contábil só serve para o
/// <see cref="Extrato"/> conferir os saldos e não é gravada na transação, que usa a data do lançamento.
/// </summary>
public sealed class LancamentoContabilizado : ValueObject
{
    public Lancamento Lancamento { get; }
    public DateOnly DataContabil { get; }

    private LancamentoContabilizado(Lancamento lancamento, DateOnly dataContabil)
    {
        Lancamento = lancamento;
        DataContabil = dataContabil;
    }

    public static LancamentoContabilizado Criar(Lancamento lancamento, DateOnly dataContabil) => new(lancamento, dataContabil);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Lancamento;
        yield return DataContabil;
    }
}
