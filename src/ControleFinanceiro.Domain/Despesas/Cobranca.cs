using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Domain.Despesas;

/// <summary>
/// A compra lançada na fatura do cartão: a transação do cartão que cobrou a despesa e o valor que ela fixou. Cobrar não é
/// pagar: quem paga a despesa do cartão é a saída da conta que pagou a fatura.
/// </summary>
public sealed class Cobranca : ValueObject
{
    public IdTransacao IdTransacao { get; }
    public ReaisPositivo Valor { get; }

    public Cobranca(IdTransacao idTransacao, ReaisPositivo valor)
    {
        IdTransacao = idTransacao;
        Valor = valor;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return IdTransacao;
        yield return Valor;
    }
}
