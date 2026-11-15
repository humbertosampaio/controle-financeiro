using ControleFinanceiro.Application.IA;
using ControleFinanceiro.Application.IA.Escritas;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Importacoes;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ControleFinanceiro.Mcp.Ferramentas;

/// <summary>Prevê, sem gravar, a importação de um arquivo do layout <see cref="LayoutArquivoImportacao.C6ContaCsv"/>.</summary>
[McpServerToolType]
internal sealed class PreverImportacaoC6ContaCsvTool
{
    [McpServerTool(Name = Tools.PreverImportacaoC6ContaCsv, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description($"{DescricoesImportacao.AberturaPrevia} o extrato CSV da conta do C6, pelo caminho. {DescricoesImportacao.RetornoPreviaExtrato} {DescricoesImportacao.FechoPrevia} {Tools.ConfirmarImportacaoC6ContaCsv} {DescricoesImportacao.FimPrevia}")]
    public static Task<ImportacaoParaModelo> Prever(
        IOrigemArquivoImportacao origem,
        ImportacaoExtratoParaModeloService importacaoExtrato,
        [Description(DescricoesImportacao.Caminho)] string caminho,
        CancellationToken cancellationToken = default)
    {
        return ArquivoDaOrigem.ImportarAsync(
            origem,
            caminho,
            arquivo => importacaoExtrato.PreverAsync(arquivo, LayoutArquivoImportacao.C6ContaCsv, cancellationToken),
            cancellationToken);
    }
}
