using ControleFinanceiro.Application.IA.Escritas;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Application.Importacoes;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ControleFinanceiro.Mcp.Ferramentas;

/// <summary>
/// Grava a importação que o usuário aprovou no cliente, como a <see cref="ConfirmarPagamentoFaturaTool"/>: a meta pede
/// ao cliente a aprovação do usuário em toda chamada, e o resumo vem primeiro nos argumentos para ser o que ele lê
/// primeiro no pedido. O caminho, que o resumo não traz, fica nos argumentos, que o usuário também vê. O sufixo da
/// conta é opcional com o texto vazio, pelo mesmo motivo da <see cref="PreverImportacaoTool"/>.
/// </summary>
[McpServerToolType]
internal sealed class ConfirmarImportacaoTool
{
    [McpServerTool(Name = "confirmar_importacao", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [McpMeta("anthropic/requiresUserInteraction", true)]
    [Description("Grava a importação de um arquivo de extrato ou de fatura, depois de prever_importacao. O usuário aprova a chamada no cliente, lendo o resumo; sem a aprovação, nada é gravado. Use os mesmos argumentos da prévia e o resumo dela sem alteração: o arquivo é lido de novo, e se a importação recalculada não tiver esse resumo, por exemplo porque o arquivo mudou ou já foi importado, nada é gravado. Devolve o mesmo que a prévia, ou o erro.")]
    public static Task<ImportacaoParaModelo> Confirmar(
        IOrigemArquivoImportacao origem,
        ImportacaoParaModeloService importacao,
        [Description("O resumo devolvido por prever_importacao, sem alteração.")] string resumo,
        [Description("O caminho do arquivo, o mesmo da prévia.")] string caminho,
        [Description("O código do layout, o mesmo da prévia.")] string layout,
        [Description("O sufixo da conta, o mesmo da prévia; só no layout mercado-pago-conta-csv.")] string sufixoConta = "",
        CancellationToken cancellationToken = default)
    {
        return ArquivoDaOrigem.ImportarAsync(
            origem,
            caminho,
            arquivo => importacao.ConfirmarAsync(arquivo, layout, sufixoConta, resumo, cancellationToken),
            cancellationToken);
    }
}
