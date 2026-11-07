using System.ComponentModel;
using ControleFinanceiro.Application.IA.Escritas;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Mcp.Confirmacao;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace ControleFinanceiro.Mcp.Ferramentas;

[McpServerToolType]
internal sealed class VincularTransacaoTool
{
    [McpServerTool(Name = "vincular_transacao", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Registra que uma saída pagou uma despesa, pelos ids de listar_transacoes e listar_despesas_fixas. Antes de gravar, o usuário vê o vínculo e confirma; sem a confirmação dele, nada é gravado. Devolve a transação, a despesa já paga e o valor previsto, ou o erro.")]
    public static Task<VinculoParaModelo> Vincular(
        VinculoParaModeloService vinculo,
        McpServer servidor,
        ILogger<ConfirmacaoPorElicitacao> logger,
        [Description("O id da transação, uma saída.")] Guid idTransacao,
        [Description("O id da despesa do mês que a transação pagou.")] Guid idDespesa,
        CancellationToken cancellationToken)
    {
        return vinculo.VincularAsync(idTransacao, idDespesa, new ConfirmacaoPorElicitacao(servidor, logger), cancellationToken);
    }
}
