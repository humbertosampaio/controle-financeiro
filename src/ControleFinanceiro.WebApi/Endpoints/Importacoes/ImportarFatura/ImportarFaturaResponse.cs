namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarFatura;

/// <summary>
/// O resumo da importação, com uma linha por cartão, na ordem em que os finais aparecem no arquivo, e o aviso, que não
/// impede a gravação. Nada do conteúdo dos lançamentos volta na resposta.
/// </summary>
/// <param name="AvisoPagamentoFaturaAnterior">Presente quando a fatura não traz o pagamento da anterior.</param>
public sealed record class ImportarFaturaResponse(
    IReadOnlyList<ImportarFaturaResponse.CartaoImportado> Cartoes,
    string? AvisoPagamentoFaturaAnterior)
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
