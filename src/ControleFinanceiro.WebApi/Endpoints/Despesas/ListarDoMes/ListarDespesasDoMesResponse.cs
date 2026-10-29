namespace ControleFinanceiro.WebApi.Endpoints.Despesas.ListarDoMes;

/// <param name="Despesas">As despesas do usuário no mês, por data de ocorrência e descrição.</param>
public sealed record class ListarDespesasDoMesResponse(IReadOnlyList<ListarDespesasDoMesResponse.Despesa> Despesas)
{
    /// <param name="FormaPagamento">O código da forma de pagamento: 1 (Pix), 2 (crédito) ou 3 (boleto).</param>
    /// <param name="IdCartao">Só no crédito.</param>
    /// <param name="IdDespesaRecorrente">A recorrente que gerou a despesa, quando ela veio de uma.</param>
    /// <param name="StatusPagamento">O código do status do pagamento: 0 (pendente) ou 1 (pago).</param>
    public sealed record class Despesa(
        Guid Id,
        Guid IdBeneficiario,
        string Descricao,
        Guid? IdCategoria,
        DateOnly DataOcorrencia,
        int FormaPagamento,
        Guid? IdCartao,
        Guid? IdDespesaRecorrente,
        decimal Valor,
        int StatusPagamento,
        DateOnly? DataPagamentoProjetada,
        DateOnly? DataPagamentoEfetiva);
}
