using Ardalis.SmartEnum;

namespace ControleFinanceiro.Domain.Contas;

public sealed class TipoConta : SmartEnum<TipoConta>
{
    public static readonly TipoConta ContaCorrente = new(1, "Conta corrente");
    public static readonly TipoConta ContaPagamento = new(2, "Conta de pagamento");

    private TipoConta(int value, string name) : base(name, value)
    {
    }
}
