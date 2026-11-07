using ControleFinanceiro.Domain.Shared;

namespace ControleFinanceiro.Domain.Despesas;

/// <summary>A saída da conta não pagou a despesa nem nenhuma das parcelas dela.</summary>
public sealed class ErroTransacaoNaoPagouADespesa : Erro
{
    public static readonly ErroTransacaoNaoPagouADespesa Instancia = new();

    internal ErroTransacaoNaoPagouADespesa()
        : base("Despesas.TransacaoNaoPagouADespesa", "A transação não pagou essa despesa.")
    {
    }
}
