using ControleFinanceiro.Domain.Shared;

namespace ControleFinanceiro.Domain.Despesas;

/// <summary>A despesa ou a parcela já foi quitada por uma transação. Para trocar, o vínculo é desfeito antes.</summary>
public sealed class ErroDespesaJaPaga : Erro
{
    public static readonly ErroDespesaJaPaga Instancia = new();

    internal ErroDespesaJaPaga()
        : base("Despesas.JaPaga", "A despesa já foi quitada por outra transação; desfaça o vínculo antes de trocar.")
    {
    }
}
