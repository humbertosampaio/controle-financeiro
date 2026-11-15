using ControleFinanceiro.Application.IA;
using ControleFinanceiro.Application.IA.Escritas;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Importacoes;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ControleFinanceiro.Mcp.Ferramentas;

/// <summary>Prevê, sem gravar, a importação de um arquivo do layout <see cref="LayoutArquivoImportacao.MercadoPagoContaCsv"/>.</summary>
[McpServerToolType]
internal sealed class PreverImportacaoMercadoPagoContaCsvTool
{
    [McpServerTool(Name = Tools.PreverImportacaoMercadoPagoContaCsv, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description($"{DescricoesImportacao.AberturaPrevia} o extrato CSV da conta do Mercado Pago, pelo caminho e pelo sufixo da conta. {DescricoesImportacao.RetornoPreviaExtrato} {DescricoesImportacao.FechoPrevia} {Tools.ConfirmarImportacaoMercadoPagoContaCsv} {DescricoesImportacao.FimPrevia}")]
    public static Task<ImportacaoParaModelo> Prever(
        IOrigemArquivoImportacao origem,
        ImportacaoExtratoParaModeloService importacaoExtrato,
        [Description(DescricoesImportacao.Caminho)] string caminho,
        [Description(DescricoesImportacao.SufixoConta)] string sufixoConta,
        CancellationToken cancellationToken = default)
    {
        return ArquivoDaOrigem.ImportarAsync(
            origem,
            caminho,
            arquivo => importacaoExtrato.PreverContaInformadaAsync(arquivo, LayoutArquivoImportacao.MercadoPagoContaCsv, sufixoConta, cancellationToken),
            cancellationToken);
    }
}
