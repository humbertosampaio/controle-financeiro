namespace ControleFinanceiro.Domain.Despesas;

/// <summary>O que um pagamento da fatura fez.</summary>
/// <param name="Pagas">As despesas que a saída pagou nesta chamada, inteiras ou pela parcela do mês.</param>
public sealed record class PagamentoFatura(IReadOnlyList<Despesa> Pagas);
