using ControleFinanceiro.Domain.Shared;

namespace ControleFinanceiro.Domain.Despesas;

/// <summary>A transação veio de uma origem que não paga a despesa: a simples é paga por uma conta, e a adiada e a parcelada, pelo cartão delas.</summary>
public sealed class ErroOrigemIncompativelComDespesa : Erro
{
    public static readonly ErroOrigemIncompativelComDespesa Instancia = new();

    internal ErroOrigemIncompativelComDespesa()
        : base("Despesas.OrigemIncompativel", "A transação não veio da origem que paga essa despesa: a simples é paga por uma conta, e a adiada e a parcelada, pelo cartão delas.")
    {
    }
}
