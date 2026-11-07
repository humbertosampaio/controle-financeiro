using System.ComponentModel;
using ControleFinanceiro.Application.IA.Escritas;
using ControleFinanceiro.Application.IA.Saida;
using ModelContextProtocol.Server;

namespace ControleFinanceiro.Mcp.Ferramentas;

/// <summary>
/// Grava o vínculo que o usuário aprovou no cliente. A meta pede ao cliente a aprovação do usuário em toda chamada, e o
/// resumo vem primeiro nos argumentos para ser o que ele lê primeiro no pedido.
/// </summary>
[McpServerToolType]
internal sealed class ConfirmarVinculoTool
{
    [McpServerTool(Name = "confirmar_vinculo", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [McpMeta("anthropic/requiresUserInteraction", true)]
    [Description("Grava o vínculo de uma saída a uma despesa, depois de prever_vinculo. O usuário aprova a chamada no cliente, lendo o resumo; sem a aprovação, nada é gravado. Use os mesmos ids da prévia e o resumo dela sem alteração: se o vínculo recalculado não tiver esse resumo, nada é gravado. Devolve a transação, a despesa já paga, o valor previsto e o resumo, ou o erro.")]
    public static Task<VinculoParaModelo> Confirmar(
        VinculoParaModeloService vinculo,
        [Description("O resumo devolvido por prever_vinculo, sem alteração.")] string resumo,
        [Description("O id da transação, o mesmo da prévia.")] Guid idTransacao,
        [Description("O id da despesa, o mesmo da prévia.")] Guid idDespesa,
        CancellationToken cancellationToken)
    {
        return vinculo.ConfirmarAsync(idTransacao, idDespesa, resumo, cancellationToken);
    }
}
