using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using CSharpFunctionalExtensions.ValueTasks;

namespace ControleFinanceiro.Domain.Services;

public static class DespesaFactory
{
    /// <summary>A despesa do mês de uma recorrente paga sem cartão: a data e o pagamento ficam no próprio mês.</summary>
    /// <exception cref="ArgumentException">Quando a despesa recorrente é no crédito.</exception>
    public static Result<Despesa> CriarComPagamentoImediato(DespesaRecorrente despesaRecorrente, MesReferencia mesReferencia)
    {
        if (despesaRecorrente.FormaPagamento == FormaPagamento.Credito)
            throw new ArgumentException("A despesa recorrente no crédito precisa do cartão.", nameof(despesaRecorrente));

        DateTimeOffset dataCompra = mesReferencia.NoDia(despesaRecorrente.DiaCompra).ToLocalDateTimeOffset();
        return DespesaSimples
            .Criar(
                despesaRecorrente.IdUsuario,
                despesaRecorrente.IdBeneficiario,
                despesaRecorrente.Descricao,
                despesaRecorrente.IdCategoria,
                dataCompra,
                despesaRecorrente.FormaPagamento,
                despesaRecorrente.Valor)
            .Map(despesa => despesa as Despesa);
    }

    /// <summary>
    /// A despesa do mês de uma recorrente no crédito: a data fica no mês anterior, e o pagamento, no vencimento
    /// do cartão no mês de referência.
    /// </summary>
    /// <param name="cartao">
    /// O cartão da despesa recorrente. A posse foi conferida na criação da recorrente: outro cartão é erro de
    /// quem chama.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Quando a despesa recorrente não é no crédito ou o cartão não é o dela.
    /// </exception>
    public static Result<Despesa> CriarComPagamentoAdiado(DespesaRecorrente despesaRecorrente, Cartao cartao, MesReferencia mesReferencia)
    {
        if (despesaRecorrente.FormaPagamento != FormaPagamento.Credito)
            throw new ArgumentException("Só a despesa recorrente no crédito tem cartão.", nameof(despesaRecorrente));

        if (cartao.Id != despesaRecorrente.IdCartao)
            throw new ArgumentException("O cartão não é o da despesa recorrente.", nameof(cartao));

        DateTimeOffset dataCompra = mesReferencia.Anterior()
            .NoDia(despesaRecorrente.DiaCompra)
            .ToLocalDateTimeOffset();

        return DespesaAdiada
            .Criar(
                despesaRecorrente.IdUsuario,
                despesaRecorrente.IdBeneficiario,
                despesaRecorrente.Descricao,
                despesaRecorrente.IdCategoria,
                dataCompra,
                despesaRecorrente.Valor,
                cartao.DiaVencimento);
    }
}
