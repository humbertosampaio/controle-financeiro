using System.ComponentModel;
using ControleFinanceiro.Application.IA.Escritas;
using ControleFinanceiro.Application.IA.Saida;
using ModelContextProtocol.Server;

namespace ControleFinanceiro.Mcp.Ferramentas;

[McpServerToolType]
internal sealed class PreverVinculoTool
{
    [McpServerTool(Name = "prever_vinculo", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Mostra, sem gravar, como fica o vínculo de uma saída a uma despesa, pelos ids de listar_transacoes e listar_despesas_fixas. Devolve a transação, a despesa já paga, o valor previsto e o resumo do vínculo, ou o erro. Nada é gravado: mostre o resumo ao usuário e, se ele quiser o vínculo, chame confirmar_vinculo com os mesmos ids e o resumo sem alteração.")]
    public static Task<VinculoParaModelo> Prever(
        VinculoParaModeloService vinculo,
        [Description("O id da transação, uma saída.")] Guid idTransacao,
        [Description("O id da despesa do mês que a transação pagou.")] Guid idDespesa,
        CancellationToken cancellationToken)
    {
        return vinculo.PreverAsync(idTransacao, idDespesa, cancellationToken);
    }
}
