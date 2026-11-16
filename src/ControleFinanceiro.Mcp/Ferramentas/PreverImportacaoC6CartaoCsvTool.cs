using ControleFinanceiro.Application.IA;
using ControleFinanceiro.Application.IA.Escritas;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Importacoes;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ControleFinanceiro.Mcp.Ferramentas;

/// <summary>Prevê, sem gravar, a importação de um arquivo do layout <see cref="LayoutArquivoImportacao.C6CartaoCsv"/>.</summary>
[McpServerToolType]
internal sealed class PreverImportacaoC6CartaoCsvTool
{
    [McpServerTool(Name = Tools.PreverImportacaoC6CartaoCsv, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description($"{DescricoesImportacao.AberturaPrevia} a fatura CSV do cartão do C6, pelo caminho e pelo vencimento da fatura. {DescricoesImportacao.RetornoPreviaFatura} {DescricoesImportacao.FechoPrevia} {Tools.ConfirmarImportacaoC6CartaoCsv} {DescricoesImportacao.FimPrevia}")]
    public static Task<ImportacaoParaModelo> Prever(
        IOrigemArquivoImportacao origem,
        ImportacaoFaturaParaModeloService importacaoFatura,
        [Description(DescricoesImportacao.Caminho)] string caminho,
        [Description(DescricoesImportacao.Vencimento)] string vencimento,
        CancellationToken cancellationToken = default)
    {
        return ArquivoDaOrigem.ImportarAsync(
            origem,
            caminho,
            arquivo => importacaoFatura.PreverAsync(arquivo, LayoutArquivoImportacao.C6CartaoCsv, new ParametrosImportacaoFaturaDto(vencimento), cancellationToken),
            cancellationToken);
    }
}
