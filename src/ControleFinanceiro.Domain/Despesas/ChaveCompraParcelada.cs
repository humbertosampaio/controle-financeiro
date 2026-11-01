using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Domain.Despesas;

/// <summary>
/// O que reconhece a mesma compra parcelada entre faturas: cartão, data, descrição normalizada e total de parcelas. O valor
/// fica de fora porque parcelas da mesma compra podem ter valores diferentes. Compras gêmeas têm a mesma chave.
/// </summary>
public sealed class ChaveCompraParcelada : ValueObject
{
    public IdCartao IdCartao { get; }
    public DateOnly Data { get; }
    public TextoNormalizado Descricao { get; }
    public int TotalParcelas { get; }

    public ChaveCompraParcelada(IdCartao idCartao, DateOnly data, TextoNormalizado descricao, int totalParcelas)
    {
        IdCartao = idCartao;
        Data = data;
        Descricao = descricao;
        TotalParcelas = totalParcelas;
    }

    /// <param name="transacao">Uma transação parcelada, o que quem chama já conferiu. Só a fatura traz parcela, então a origem é sempre um cartão.</param>
    internal static ChaveCompraParcelada DaTransacao(Transacao transacao) =>
        new(transacao.Origem.IdCartao!.Value, transacao.Data, (TextoNormalizado)transacao.Descricao, transacao.Parcela!.Total);

    /// <param name="lancamento">Uma compra parcelada da fatura do cartão, o que quem chama já conferiu.</param>
    internal static ChaveCompraParcelada DoLancamento(IdCartao idCartao, Lancamento lancamento) =>
        new(idCartao, lancamento.Data, (TextoNormalizado)lancamento.Descricao, lancamento.Parcela!.Total);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return IdCartao;
        yield return Data;
        yield return Descricao;
        yield return TotalParcelas;
    }
}
