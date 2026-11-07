namespace ControleFinanceiro.WebApi.Endpoints.Despesas.ListarDoMes;

/// <param name="Despesas">As despesas do usuário no mês, por data de ocorrência e descrição.</param>
public sealed record class ListarDespesasDoMesResponse(IReadOnlyList<ListarDespesasDoMesResponse.Despesa> Despesas)
{
    /// <param name="FormaPagamento">O código da forma de pagamento: 1 (Pix), 2 (crédito) ou 3 (boleto).</param>
    /// <param name="IdCartao">Só no crédito.</param>
    /// <param name="IdDespesaRecorrente">A recorrente que gerou a despesa, quando ela veio de uma.</param>
    /// <param name="ValorProjetado">Na parcelada, o valor da parcela do mês, assim como os demais dados do pagamento.</param>
    /// <param name="ValorEfetivo">O que de fato saiu, só quando está pago.</param>
    /// <param name="StatusPagamento">O código do status do pagamento: 0 (pendente) ou 1 (pago).</param>
    /// <param name="IdTransacao">A transação que quitou, só quando há vínculo.</param>
    /// <param name="Parcela">A parcela do mês, só na despesa parcelada.</param>
    public sealed record class Despesa(
        Guid Id,
        Guid IdBeneficiario,
        string Descricao,
        Guid? IdCategoria,
        DateOnly DataOcorrencia,
        int FormaPagamento,
        Guid? IdCartao,
        Guid? IdDespesaRecorrente,
        decimal ValorProjetado,
        decimal? ValorEfetivo,
        int StatusPagamento,
        DateOnly DataPagamentoProjetada,
        DateOnly? DataPagamentoEfetiva,
        Guid? IdTransacao,
        ListarDespesasDoMesResponse.ParcelaDoMes? Parcela);

    /// <param name="Total">O total de parcelas da compra.</param>
    public sealed record class ParcelaDoMes(Guid Id, int Numero, int Total);
}
