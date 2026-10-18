using Ardalis.SmartEnum;

namespace ControleFinanceiro.Domain.Shared.ValueObjects;

public sealed class StatusPagamento : SmartEnum<StatusPagamento>
{
    public static readonly StatusPagamento Pendente = new("Pendente", 0);
    public static readonly StatusPagamento Pago = new("Pago", 1);

    private StatusPagamento(string name, int value) : base(name, value)
    {
    }
}
