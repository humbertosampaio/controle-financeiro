using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Domain.Despesas;

/// <summary>A parcela de um número numa compra. Compras gêmeas têm a mesma, uma em cada despesa.</summary>
internal sealed class ParcelaDaCompra : ValueObject
{
    public ChaveCompraParcelada Chave { get; }
    public int Numero { get; }

    private ParcelaDaCompra(ChaveCompraParcelada chave, int numero)
    {
        Chave = chave;
        Numero = numero;
    }

    /// <param name="transacao">Uma transação com <see cref="TransacaoCartao.EhParcelaPaga"/>, que quem chama já filtrou.</param>
    public static ParcelaDaCompra DaTransacao(TransacaoCartao transacao) =>
        new(ChaveCompraParcelada.DaTransacao(transacao), transacao.Parcela!.Numero);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Chave;
        yield return Numero;
    }
}
