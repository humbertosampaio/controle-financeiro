namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.PreVisualizarExtrato;

/// <summary>
/// O que o usuário precisa resolver antes de importar o extrato. Nada dos valores nem dos lançamentos volta na
/// resposta.
/// </summary>
/// <param name="Beneficiarios">
/// Os textos de beneficiário das saídas que ainda não são alias de nenhum beneficiário do usuário, um por forma
/// normalizada, como apareceram pela primeira vez no arquivo e na ordem dele.
/// </param>
public sealed record class PreVisualizarExtratoResponse(IReadOnlyList<string> Beneficiarios);
