using ControleFinanceiro.Domain.Shared;

namespace ControleFinanceiro.Domain.Despesas;

/// <summary>
/// A despesa ou a parcela já foi paga por uma transação: a simples por outra saída, a do cartão pela fatura. Para trocar
/// a saída da simples, ou para tirar a cobrança da despesa do cartão, o pagamento é desfeito antes.
/// </summary>
public sealed class ErroDespesaJaPaga : Erro
{
    public static readonly ErroDespesaJaPaga Instancia = new();

    internal ErroDespesaJaPaga()
        : base("Despesas.JaPaga", "A despesa já foi paga por outra transação; desfaça o pagamento antes.")
    {
    }
}
