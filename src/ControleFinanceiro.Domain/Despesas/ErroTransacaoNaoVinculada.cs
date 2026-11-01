using ControleFinanceiro.Domain.Shared;

namespace ControleFinanceiro.Domain.Despesas;

/// <summary>A transação não quitou a despesa nem nenhuma das parcelas dela.</summary>
public sealed class ErroTransacaoNaoVinculada : Erro
{
    public static readonly ErroTransacaoNaoVinculada Instancia = new();

    internal ErroTransacaoNaoVinculada()
        : base("Despesas.TransacaoNaoVinculada", "A transação não está vinculada a essa despesa.")
    {
    }
}
