using ControleFinanceiro.Domain.Shared;

namespace ControleFinanceiro.Domain.Despesas;

/// <summary>
/// A saída já pagou despesas de outra fatura: de um cartão que não sai na mesma fatura, ou de outro mês de vencimento.
/// Uma saída paga uma fatura só.
/// </summary>
public sealed class ErroTransacaoPagaOutraFatura : Erro
{
    public static readonly ErroTransacaoPagaOutraFatura Instancia = new();

    internal ErroTransacaoPagaOutraFatura()
        : base("Despesas.TransacaoPagaOutraFatura", "A saída já pagou outra fatura; desfaça esse pagamento antes de trocar.")
    {
    }
}
