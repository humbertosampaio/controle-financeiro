namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarFatura;

/// <summary>
/// O resumo da importação, com uma linha por cartão, na ordem em que os finais aparecem no arquivo. Nada do conteúdo
/// dos lançamentos volta na resposta.
/// </summary>
public sealed record class ImportarFaturaResponse(IReadOnlyList<ImportarFaturaResponse.CartaoImportado> Cartoes)
{
    /// <param name="CartaoNovo">Verdadeiro quando o cartão foi criado por esta importação.</param>
    /// <param name="VencimentoPadrao">
    /// Verdadeiro quando o cartão novo ficou com o dia de vencimento padrão, porque nenhum dia foi informado para ele.
    /// </param>
    public sealed record class CartaoImportado(
        Guid IdImportacao,
        Guid IdCartao,
        string Sufixo,
        bool CartaoNovo,
        bool VencimentoPadrao,
        int Lidas,
        int Novas,
        int JaExistentes);
}
