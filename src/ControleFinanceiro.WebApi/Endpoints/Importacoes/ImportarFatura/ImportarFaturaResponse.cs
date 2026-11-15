namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarFatura;

/// <summary>
/// O resumo da importação, com o vencimento, uma linha por cartão, na ordem em que os finais aparecem no arquivo, e os
/// avisos, que não impedem a gravação. Nada do conteúdo dos lançamentos volta na resposta.
/// </summary>
/// <param name="DataVencimento">A data em que a fatura vence: a do arquivo ou a informada.</param>
/// <param name="AvisoPagamentoFaturaAnterior">Presente quando a fatura não traz o pagamento da anterior.</param>
/// <param name="AvisoVencimentoForaCiclo">Presente quando o vencimento não fecha com o ciclo cadastrado de algum cartão.</param>
public sealed record class ImportarFaturaResponse(
    DateOnly DataVencimento,
    IReadOnlyList<ImportarFaturaResponse.CartaoImportado> Cartoes,
    string? AvisoPagamentoFaturaAnterior,
    string? AvisoVencimentoForaCiclo)
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
}
