namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarFatura;

/// <summary>
/// O resumo da importação, com uma linha por cartão, na ordem em que os finais aparecem no arquivo. Nada do conteúdo
/// dos lançamentos volta na resposta.
/// </summary>
public sealed record class ImportarFaturaResponse(IReadOnlyList<ImportarFaturaResponse.CartaoImportado> Cartoes)
{
    public sealed record class CartaoImportado(
        Guid IdImportacao,
        Guid IdCartao,
        string Sufixo,
        int Lidas,
        int Novas,
        int JaExistentes);
}
