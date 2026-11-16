using ControleFinanceiro.Application.IA;
using ControleFinanceiro.Application.IA.Escritas;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Importacoes;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ControleFinanceiro.Mcp.Ferramentas;

/// <summary>Prevê, sem gravar, a importação de um arquivo do layout <see cref="LayoutArquivoImportacao.MercadoPagoCartaoCsv"/>.</summary>
[McpServerToolType]
internal sealed class PreverImportacaoMercadoPagoCartaoCsvTool
{
    [McpServerTool(Name = Tools.PreverImportacaoMercadoPagoCartaoCsv, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description($"{DescricoesImportacao.AberturaPrevia} a fatura CSV do cartão do Mercado Pago, pelo caminho. {DescricoesImportacao.RetornoPreviaFatura} {DescricoesImportacao.FechoPrevia} {Tools.ConfirmarImportacaoMercadoPagoCartaoCsv} {DescricoesImportacao.FimPrevia}")]
    public static Task<ImportacaoParaModelo> Prever(
        IOrigemArquivoImportacao origem,
        ImportacaoFaturaParaModeloService importacaoFatura,
        [Description(DescricoesImportacao.Caminho)] string caminho,
        CancellationToken cancellationToken = default)
    {
        return ArquivoDaOrigem.ImportarAsync(
            origem,
            caminho,
            arquivo => importacaoFatura.PreverAsync(arquivo, LayoutArquivoImportacao.MercadoPagoCartaoCsv, new ParametrosImportacaoFaturaDto(Vencimento: null), cancellationToken),
            cancellationToken);
    }
}
