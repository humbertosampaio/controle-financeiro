using ControleFinanceiro.Application.IA;
using ControleFinanceiro.Application.IA.Leituras;
using ControleFinanceiro.Application.IA.Saida;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ControleFinanceiro.Mcp.Ferramentas;

[McpServerToolType]
internal sealed class ListarTransacoesTool
{
    [McpServerTool(Name = Tools.ListarTransacoes, ReadOnly = true)]
    [Description("Lista as transações de um mês, das contas e dos cartões, em ordem de data: id, data, valor (negativo é saída), descrição e contraparte.")]
    public static Task<TransacoesDoMesParaModelo> Listar(
        LeituraParaModeloService leitura,
        [Description("O ano, com quatro dígitos.")] int ano,
        [Description("O mês, de 1 a 12.")] int mes,
        CancellationToken cancellationToken)
    {
        return leitura.ListarTransacoesAsync(ano, mes, cancellationToken);
    }
}
