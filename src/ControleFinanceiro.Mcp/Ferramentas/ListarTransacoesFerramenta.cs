using System.ComponentModel;
using ControleFinanceiro.Application.Leituras;
using ControleFinanceiro.Application.SaidaModelo;
using ModelContextProtocol.Server;

namespace ControleFinanceiro.Mcp.Ferramentas;

[McpServerToolType]
internal sealed class ListarTransacoesFerramenta
{
    [McpServerTool(Name = "listar_transacoes", ReadOnly = true)]
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
