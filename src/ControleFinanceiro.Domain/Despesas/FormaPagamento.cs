using Ardalis.SmartEnum;

namespace ControleFinanceiro.Domain.Despesas;

public sealed class FormaPagamento : SmartEnum<FormaPagamento>
{
    public static readonly FormaPagamento Pix = new(1, "Pix");
    public static readonly FormaPagamento Credito = new(2, "Cartão de Crédito");
    public static readonly FormaPagamento Boleto = new(3, "Boleto");

    private FormaPagamento(int value, string name) : base(name, value)
    {
    }
}