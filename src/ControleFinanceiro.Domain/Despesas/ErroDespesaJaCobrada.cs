using ControleFinanceiro.Domain.Shared;

namespace ControleFinanceiro.Domain.Despesas;

/// <summary>A despesa ou a parcela já foi cobrada por uma compra da fatura. Para trocar, a cobrança é desfeita antes.</summary>
public sealed class ErroDespesaJaCobrada : Erro
{
    public static readonly ErroDespesaJaCobrada Instancia = new();

    internal ErroDespesaJaCobrada()
        : base("Despesas.JaCobrada", "A despesa já foi cobrada por outra compra da fatura; desfaça a cobrança antes de trocar.")
    {
    }
}
