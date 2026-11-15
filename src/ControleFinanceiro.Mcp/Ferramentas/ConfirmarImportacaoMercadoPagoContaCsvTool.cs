using ControleFinanceiro.Application.IA;
using ControleFinanceiro.Application.IA.Escritas;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Importacoes;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ControleFinanceiro.Mcp.Ferramentas;

/// <summary>
/// Grava a importação de um arquivo do layout <see cref="LayoutArquivoImportacao.MercadoPagoContaCsv"/> que o usuário
/// aprovou no cliente, como a <see cref="ConfirmarPagamentoFaturaTool"/>: a meta pede ao cliente a aprovação do usuário
/// em toda chamada, e o resumo vem primeiro nos argumentos para ser o que ele lê primeiro no pedido. O caminho, que o
/// resumo não traz, fica nos argumentos, que o usuário também vê.
/// </summary>
[McpServerToolType]
internal sealed class ConfirmarImportacaoMercadoPagoContaCsvTool
{
    [McpServerTool(Name = Tools.ConfirmarImportacaoMercadoPagoContaCsv, ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [McpMeta("anthropic/requiresUserInteraction", true)]
    [Description($"Grava a importação do extrato CSV da conta do Mercado Pago, depois de {Tools.PreverImportacaoMercadoPagoContaCsv}. {DescricoesImportacao.FechoConfirmacao}")]
    public static Task<ImportacaoParaModelo> Confirmar(
        IOrigemArquivoImportacao origem,
        ImportacaoExtratoParaModeloService importacaoExtrato,
        [Description($"O resumo devolvido por {Tools.PreverImportacaoMercadoPagoContaCsv}, sem alteração.")] string resumo,
        [Description(DescricoesImportacao.CaminhoConfirmacao)] string caminho,
        [Description(DescricoesImportacao.SufixoContaConfirmacao)] string sufixoConta,
        CancellationToken cancellationToken = default)
    {
        return ArquivoDaOrigem.ImportarAsync(
            origem,
            caminho,
            arquivo => importacaoExtrato.ConfirmarContaInformadaAsync(arquivo, LayoutArquivoImportacao.MercadoPagoContaCsv, sufixoConta, resumo, cancellationToken),
            cancellationToken);
    }
}
