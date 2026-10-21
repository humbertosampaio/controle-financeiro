using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Domain.Importacoes;

/// <summary>
/// Um lançamento do extrato com o dia contábil em que ele entrou no saldo. A data contábil só serve para a
/// <see cref="ConferenciaSaldo"/> e não é gravada na transação, que usa a data do lançamento.
/// </summary>
public sealed class LancamentoLido
{
    public LancamentoExtrato Lancamento { get; }
    public DateOnly DataContabil { get; }

    private LancamentoLido(LancamentoExtrato lancamento, DateOnly dataContabil)
    {
        Lancamento = lancamento;
        DataContabil = dataContabil;
    }

    public static LancamentoLido Criar(LancamentoExtrato lancamento, DateOnly dataContabil) => new(lancamento, dataContabil);
}
