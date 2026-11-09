using ControleFinanceiro.Application.IA.Escritas;
using ControleFinanceiro.Application.IA.Saida;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ControleFinanceiro.Mcp.Ferramentas;

/// <summary>
/// Grava o pagamento da fatura que o usuário aprovou no cliente, como a <see cref="ConfirmarVinculoTool"/>: a meta pede
/// ao cliente a aprovação do usuário em toda chamada, e o resumo vem primeiro nos argumentos para ser o que ele lê
/// primeiro no pedido.
/// </summary>
[McpServerToolType]
internal sealed class ConfirmarPagamentoFaturaTool
{
    [McpServerTool(Name = "confirmar_pagamento_fatura", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [McpMeta("anthropic/requiresUserInteraction", true)]
    [Description("Grava o pagamento da fatura do cartão por uma saída da conta, depois de prever_pagamento_fatura. O usuário aprova a chamada no cliente, lendo o resumo; sem a aprovação, nada é gravado. Use os mesmos argumentos da prévia e o resumo dela sem alteração: se o pagamento recalculado não tiver esse resumo, por exemplo porque uma cobrança chegou depois da prévia, nada é gravado. Devolve a saída, as despesas pagas, o total e o resumo, ou o erro.")]
    public static Task<PagamentoFaturaParaModelo> Confirmar(
        PagamentoFaturaParaModeloService pagamento,
        [Description("O resumo devolvido por prever_pagamento_fatura, sem alteração.")] string resumo,
        [Description("O id da transação, o mesmo da prévia.")] Guid idTransacao,
        [Description("O id do cartão, o mesmo da prévia.")] Guid idCartao,
        [Description("O ano do vencimento, o mesmo da prévia.")] int ano,
        [Description("O mês do vencimento, o mesmo da prévia.")] int mes,
        CancellationToken cancellationToken)
    {
        return pagamento.ConfirmarAsync(idTransacao, idCartao, ano, mes, resumo, cancellationToken);
    }
}
