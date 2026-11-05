using System.ComponentModel;
using ControleFinanceiro.Application.Leituras;
using ControleFinanceiro.Application.SaidaModelo;
using ModelContextProtocol.Server;

namespace ControleFinanceiro.Mcp.Ferramentas;

[McpServerToolType]
internal sealed class ListarDespesasFixasTool
{
    [McpServerTool(Name = "listar_despesas_fixas", ReadOnly = true)]
    [Description("Lista as despesas fixas (recorrentes) com o valor e o dia em que ocorrem, cada uma com a despesa do mês pedido e o status do pagamento (Pago ou Pendente), quando a despesa do mês já foi gerada.")]
    public static Task<DespesasFixasDoMesParaModelo> Listar(
        LeituraParaModeloService leitura,
        [Description("O ano, com quatro dígitos.")] int ano,
        [Description("O mês, de 1 a 12.")] int mes,
        CancellationToken cancellationToken)
    {
        return leitura.ListarDespesasFixasAsync(ano, mes, cancellationToken);
    }
}
