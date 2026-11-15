using ControleFinanceiro.Application.IA;
using ControleFinanceiro.Application.IA.Leituras;
using ControleFinanceiro.Application.IA.Saida;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ControleFinanceiro.Mcp.Ferramentas;

[McpServerToolType]
internal sealed class ListarCartoesTool
{
    [McpServerTool(Name = Tools.ListarCartoes, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description($"Lista os cartões de crédito do usuário, com o id, o banco, o final e os dias de fechamento e de vencimento da fatura. Os cartões do mesmo banco com os mesmos dias saem na mesma fatura, e o id de qualquer um deles serve para {Tools.PreverPagamentoFatura}.")]
    public static Task<CartoesParaModelo> Listar(LeituraParaModeloService leitura, CancellationToken cancellationToken)
    {
        return leitura.ListarCartoesAsync(cancellationToken);
    }
}
