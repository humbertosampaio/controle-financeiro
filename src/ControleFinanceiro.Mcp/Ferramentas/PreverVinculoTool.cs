using ControleFinanceiro.Application.IA;
using ControleFinanceiro.Application.IA.Escritas;
using ControleFinanceiro.Application.IA.Saida;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ControleFinanceiro.Mcp.Ferramentas;

[McpServerToolType]
internal sealed class PreverVinculoTool
{
    [McpServerTool(Name = Tools.PreverVinculo, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description($"Mostra, sem gravar, como fica o vínculo de uma saída a uma despesa, pelos ids de {Tools.ListarTransacoes} e {Tools.ListarDespesasFixas}. A saída da conta paga a despesa sem cartão; a compra do cartão cobra a despesa do crédito, que continua pendente até a fatura ser paga. Devolve a transação, a despesa como fica (paga, ou pendente com o valor cobrado), o valor previsto e o resumo do vínculo, ou o erro. Nada é gravado: mostre o resumo ao usuário e, se ele quiser o vínculo, chame {Tools.ConfirmarVinculo} com os mesmos ids e o resumo sem alteração.")]
    public static Task<VinculoParaModelo> Prever(
        VinculoParaModeloService vinculo,
        [Description("O id da transação, uma saída.")] Guid idTransacao,
        [Description("O id da despesa do mês que a transação paga ou cobra.")] Guid idDespesa,
        CancellationToken cancellationToken)
    {
        return vinculo.PreverAsync(idTransacao, idDespesa, cancellationToken);
    }
}
