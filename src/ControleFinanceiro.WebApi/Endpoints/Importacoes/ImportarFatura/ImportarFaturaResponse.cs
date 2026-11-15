namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarFatura;

/// <summary>
/// O resumo da importação, com o vencimento e uma linha por cartão, na ordem em que os finais aparecem no arquivo, os
/// avisos, que não impedem a gravação, e os candidatos a pagamento de fatura. Do conteúdo dos lançamentos, só voltam o
/// id e a data das saídas candidatas.
/// </summary>
/// <param name="DataVencimento">A data em que a fatura vence: a do arquivo ou a informada.</param>
/// <param name="AvisoPagamentoFaturaAnterior">Presente quando a fatura não traz o pagamento da anterior.</param>
/// <param name="AvisoVencimentoForaCiclo">Presente quando o vencimento não fecha com o ciclo cadastrado de algum cartão.</param>
/// <param name="CandidatosPagamentoFatura">As saídas da conta que provavelmente pagaram esta fatura, ainda sem ligação.</param>
public sealed record class ImportarFaturaResponse(
    DateOnly DataVencimento,
    IReadOnlyList<ImportarFaturaResponse.CartaoImportado> Cartoes,
    string? AvisoPagamentoFaturaAnterior,
    string? AvisoVencimentoForaCiclo,
    IReadOnlyList<ImportarFaturaResponse.CandidatoPagamentoFaturaResponse> CandidatosPagamentoFatura)
{
    public sealed record class CartaoImportado(
        Guid IdImportacao,
        Guid IdCartao,
        string Sufixo,
        int Lidas,
        int Novas,
        int JaExistentes,
        int DespesasParceladasCriadas,
        int DespesasAdiadasCriadas);

    /// <summary>
    /// A saída da conta que provavelmente pagou uma fatura, com os campos que o pagamento da fatura pede
    /// (<c>IdTransacao</c>, <c>IdCartao</c>, <c>Ano</c> e <c>Mes</c>), para ligar com um clique.
    /// </summary>
    /// <param name="IdCartao">Um cartão da fatura: o de menor id.</param>
    /// <param name="Ano">O ano do vencimento da fatura.</param>
    /// <param name="Mes">O mês do vencimento da fatura.</param>
    /// <param name="DataVencimento">O vencimento real, que a importação da fatura gravou.</param>
    /// <param name="Total">O que a fatura cobra, igual ao valor da saída.</param>
    public sealed record class CandidatoPagamentoFaturaResponse(
        Guid IdTransacao,
        DateOnly DataSaida,
        Guid IdCartao,
        string Banco,
        int Ano,
        int Mes,
        DateOnly DataVencimento,
        decimal Total);
}
