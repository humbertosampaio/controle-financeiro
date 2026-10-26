namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.PreVisualizarFatura;

/// <summary>
/// O que o usuário precisa preencher ou resolver antes de importar a fatura. Nada dos valores nem dos lançamentos volta
/// na resposta.
/// </summary>
/// <param name="FinaisCartao">Os finais de cartão do arquivo, na ordem em que aparecem pela primeira vez.</param>
/// <param name="Beneficiarios">
/// Os textos de beneficiário das compras que ainda não são alias de nenhum beneficiário do usuário, um por forma
/// normalizada, como apareceram pela primeira vez no arquivo e na ordem dele.
/// </param>
public sealed record class PreVisualizarFaturaResponse(
    IReadOnlyList<PreVisualizarFaturaResponse.FinalCartao> FinaisCartao,
    IReadOnlyList<string> Beneficiarios)
{
    /// <param name="Cadastrado">Verdadeiro quando o usuário já tem o cartão desse final no banco da fatura.</param>
    /// <param name="DiaVencimento">O dia de vencimento do cartão cadastrado; nulo no cartão que a importação vai criar.</param>
    public sealed record class FinalCartao(string Sufixo, bool Cadastrado, int? DiaVencimento);
}
