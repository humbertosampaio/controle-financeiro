using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Domain.Importacoes;

namespace ControleFinanceiro.Application.IA.Escritas;

/// <summary>
/// Temporário: o caminho antigo, com o layout em texto, que repassa ao <see cref="ImportacaoExtratoParaModeloService"/>
/// e ao <see cref="ImportacaoFaturaParaModeloService"/> até as ferramentas por layout o substituírem.
/// </summary>
public sealed class ImportacaoParaModeloService
{
    private readonly ImportacaoExtratoParaModeloService _importacaoExtratoParaModeloService;
    private readonly ImportacaoFaturaParaModeloService _importacaoFaturaParaModeloService;

    public ImportacaoParaModeloService(
        ImportacaoExtratoParaModeloService importacaoExtratoParaModeloService,
        ImportacaoFaturaParaModeloService importacaoFaturaParaModeloService)
    {
        _importacaoExtratoParaModeloService = importacaoExtratoParaModeloService;
        _importacaoFaturaParaModeloService = importacaoFaturaParaModeloService;
    }

    public async Task<ImportacaoParaModelo> PreverAsync(
        Stream arquivo,
        string layout,
        string? sufixoConta,
        string? vencimento,
        CancellationToken cancellationToken)
    {
        if (!LayoutArquivoImportacao.TryFromValue(layout, out LayoutArquivoImportacao? lido))
            return ImportacaoParaModelo.Falha(ErrosParaModelo.Importacao.LayoutInvalido);

        if (LayoutArquivoImportacao.ExtratosContaInformada.Contains(lido))
            return await _importacaoExtratoParaModeloService.PreverContaInformadaAsync(arquivo, lido, sufixoConta ?? "", cancellationToken);

        if (LayoutArquivoImportacao.FaturasVencimentoInformado.Contains(lido))
            return await _importacaoFaturaParaModeloService.PreverVencimentoInformadoAsync(arquivo, lido, vencimento ?? "", cancellationToken);

        if (!LayoutArquivoImportacao.Faturas.Contains(lido))
            return await _importacaoExtratoParaModeloService.PreverAsync(arquivo, lido, cancellationToken);

        if (!string.IsNullOrWhiteSpace(vencimento))
            return ImportacaoParaModelo.Falha(ErrosParaModelo.Importacao.VencimentoNoArquivo);

        return await _importacaoFaturaParaModeloService.PreverAsync(arquivo, lido, cancellationToken);
    }

    public async Task<ImportacaoParaModelo> ConfirmarAsync(
        Stream arquivo,
        string layout,
        string? sufixoConta,
        string? vencimento,
        string resumo,
        CancellationToken cancellationToken)
    {
        if (!LayoutArquivoImportacao.TryFromValue(layout, out LayoutArquivoImportacao? lido))
            return ImportacaoParaModelo.Falha(ErrosParaModelo.Importacao.LayoutInvalido);

        if (LayoutArquivoImportacao.ExtratosContaInformada.Contains(lido))
            return await _importacaoExtratoParaModeloService.ConfirmarContaInformadaAsync(arquivo, lido, sufixoConta ?? "", resumo, cancellationToken);

        if (LayoutArquivoImportacao.FaturasVencimentoInformado.Contains(lido))
            return await _importacaoFaturaParaModeloService.ConfirmarVencimentoInformadoAsync(arquivo, lido, vencimento ?? "", resumo, cancellationToken);

        if (!LayoutArquivoImportacao.Faturas.Contains(lido))
            return await _importacaoExtratoParaModeloService.ConfirmarAsync(arquivo, lido, resumo, cancellationToken);

        if (!string.IsNullOrWhiteSpace(vencimento))
            return ImportacaoParaModelo.Falha(ErrosParaModelo.Importacao.VencimentoNoArquivo);

        return await _importacaoFaturaParaModeloService.ConfirmarAsync(arquivo, lido, resumo, cancellationToken);
    }
}
