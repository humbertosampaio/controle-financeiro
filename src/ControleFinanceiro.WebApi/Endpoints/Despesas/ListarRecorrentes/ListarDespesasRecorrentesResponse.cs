namespace ControleFinanceiro.WebApi.Endpoints.Despesas.ListarRecorrentes;

/// <param name="DespesasRecorrentes">Todas as despesas recorrentes do usuário, por dia de ocorrência e descrição.</param>
public sealed record class ListarDespesasRecorrentesResponse(
    IReadOnlyList<ListarDespesasRecorrentesResponse.DespesaRecorrente> DespesasRecorrentes)
{
    /// <param name="FormaPagamento">O código da forma de pagamento: 1 (Pix), 2 (crédito) ou 3 (boleto).</param>
    /// <param name="IdCartao">Só no crédito.</param>
    public sealed record class DespesaRecorrente(
        Guid Id,
        Guid IdBeneficiario,
        string Descricao,
        Guid IdCategoria,
        int FormaPagamento,
        Guid? IdCartao,
        decimal Valor,
        int DiaOcorrencia);
}
