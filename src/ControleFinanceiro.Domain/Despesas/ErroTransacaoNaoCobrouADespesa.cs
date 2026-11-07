using ControleFinanceiro.Domain.Shared;

namespace ControleFinanceiro.Domain.Despesas;

/// <summary>A compra do cartão não cobrou a despesa nem nenhuma das parcelas dela.</summary>
public sealed class ErroTransacaoNaoCobrouADespesa : Erro
{
    public static readonly ErroTransacaoNaoCobrouADespesa Instancia = new();

    internal ErroTransacaoNaoCobrouADespesa()
        : base("Despesas.TransacaoNaoCobrouADespesa", "A transação não cobrou essa despesa.")
    {
    }
}
