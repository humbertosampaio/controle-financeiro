namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarExtrato;

/// <summary>
/// O resumo da importação, os avisos, que não impedem a gravação, e os candidatos a pagamento de fatura. Do conteúdo
/// dos lançamentos, só voltam o id e a data das saídas candidatas.
/// </summary>
/// <param name="PeriodoNaoImportado">Os dias entre a importação anterior da conta e esta que nenhum arquivo cobriu.</param>
/// <param name="AvisoSomaPeriodo">Presente quando a soma das transações gravadas no período não fecha com a do arquivo.</param>
/// <param name="CandidatosPagamentoFatura">As saídas do extrato que provavelmente pagaram uma fatura, ainda sem ligação.</param>
public sealed record class ImportarExtratoResponse(
    Guid IdImportacao,
    Guid IdConta,
    int Lidas,
    int Novas,
    int JaExistentes,
    ImportarExtratoResponse.Periodo? PeriodoNaoImportado,
    string? AvisoSomaPeriodo,
    IReadOnlyList<ImportarExtratoResponse.CandidatoPagamentoFaturaResponse> CandidatosPagamentoFatura)
{
    public sealed record class Periodo(DateOnly Inicio, DateOnly Fim);

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
