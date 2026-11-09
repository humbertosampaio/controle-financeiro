using ControleFinanceiro.Application.IA.Escritas;
using ControleFinanceiro.Application.IA.Saida;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ControleFinanceiro.Mcp.Ferramentas;

[McpServerToolType]
internal sealed class PreverPagamentoFaturaTool
{
    [McpServerTool(Name = "prever_pagamento_fatura", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Mostra, sem gravar, como fica o pagamento da fatura do cartão por uma saída da conta, pelo id da saída de listar_transacoes, pelo id de um cartão da fatura de listar_cartoes e pelo mês em que a fatura vence. A saída paga as despesas já cobradas e pendentes de todos os cartões da mesma fatura nesse mês, com o valor cobrado. Devolve a saída, as despesas pagas, o total e o resumo, ou o erro. Nada é gravado: mostre o resumo ao usuário e, se ele quiser o pagamento, chame confirmar_pagamento_fatura com os mesmos argumentos e o resumo sem alteração.")]
    public static Task<PagamentoFaturaParaModelo> Prever(
        PagamentoFaturaParaModeloService pagamento,
        [Description("O id da transação, uma saída da conta.")] Guid idTransacao,
        [Description("O id de um cartão da fatura.")] Guid idCartao,
        [Description("O ano em que a fatura vence, com quatro dígitos.")] int ano,
        [Description("O mês em que a fatura vence, de 1 a 12.")] int mes,
        CancellationToken cancellationToken)
    {
        return pagamento.PreverAsync(idTransacao, idCartao, ano, mes, cancellationToken);
    }
}
