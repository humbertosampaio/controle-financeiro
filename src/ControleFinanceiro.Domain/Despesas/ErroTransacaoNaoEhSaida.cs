using ControleFinanceiro.Domain.Shared;

namespace ControleFinanceiro.Domain.Despesas;

/// <summary>A transação é uma entrada ou tem valor zero, e só uma saída paga uma despesa.</summary>
public sealed class ErroTransacaoNaoEhSaida : Erro
{
    public static readonly ErroTransacaoNaoEhSaida Instancia = new();

    internal ErroTransacaoNaoEhSaida()
        : base("Despesas.TransacaoNaoEhSaida", "Só uma saída de dinheiro pode pagar uma despesa.")
    {
    }
}
