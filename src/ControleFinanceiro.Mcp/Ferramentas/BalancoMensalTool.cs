using System.ComponentModel;
using ControleFinanceiro.Application.Leituras;
using ControleFinanceiro.Application.SaidaModelo;
using ModelContextProtocol.Server;

namespace ControleFinanceiro.Mcp.Ferramentas;

[McpServerToolType]
internal sealed class BalancoMensalTool
{
    [McpServerTool(Name = "balanco_mensal", ReadOnly = true)]
    [Description("Mostra o balanço de um mês: a renda líquida atual, o total já pago e o total pendente das despesas do mês e o saldo previsto (renda líquida menos pago e pendente). A renda é a de hoje, também para meses passados.")]
    public static Task<BalancoMensalParaModelo> Obter(
        LeituraParaModeloService leitura,
        [Description("O ano, com quatro dígitos.")] int ano,
        [Description("O mês, de 1 a 12.")] int mes,
        CancellationToken cancellationToken)
    {
        return leitura.ObterBalancoAsync(ano, mes, cancellationToken);
    }
}
