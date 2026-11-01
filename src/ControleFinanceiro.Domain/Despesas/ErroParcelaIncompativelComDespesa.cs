using ControleFinanceiro.Domain.Shared;

namespace ControleFinanceiro.Domain.Despesas;

/// <summary>A transação tem parcela e a despesa é inteira, não tem parcela e a despesa é parcelada, ou é parcela de outra compra.</summary>
public sealed class ErroParcelaIncompativelComDespesa : Erro
{
    public static readonly ErroParcelaIncompativelComDespesa Instancia = new();

    internal ErroParcelaIncompativelComDespesa()
        : base("Despesas.ParcelaIncompativel", "A parcela da transação não corresponde à despesa: a parcelada só é paga por uma parcela da mesma compra, e a despesa inteira, por uma transação sem parcela.")
    {
    }
}
