using ControleFinanceiro.Domain.Shared;

namespace ControleFinanceiro.Domain.Despesas;

/// <summary>
/// Nenhuma despesa dos cartões da fatura, no mês de vencimento, tem cobrança e está pendente. Costuma ser o mês ou o
/// cartão errado.
/// </summary>
public sealed class ErroFaturaSemCobrancaPendente : Erro
{
    public static readonly ErroFaturaSemCobrancaPendente Instancia = new();

    internal ErroFaturaSemCobrancaPendente()
        : base("Despesas.FaturaSemCobrancaPendente", "Nenhuma despesa da fatura nesse mês foi cobrada e está pendente; confira o cartão e o mês.")
    {
    }
}
