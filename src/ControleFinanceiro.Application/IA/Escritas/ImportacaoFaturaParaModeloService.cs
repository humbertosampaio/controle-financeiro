using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Application.Despesas;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Usuarios;
using CSharpFunctionalExtensions;
using System.Globalization;

namespace ControleFinanceiro.Application.IA.Escritas;

/// <summary>
/// A importação de uma fatura de cartão que o modelo pede, já como tipo de saída, em duas etapas, como no
/// <see cref="ImportacaoExtratoParaModeloService"/>. A regra é a da API, pelo <see cref="ImportacaoFaturaAppService"/>,
/// aplicada em memória nas duas, e a confirmação grava pelo <see cref="ConfirmacaoImportacaoParaModeloService"/>. Um
/// par de métodos por tipo de entrada: o do layout que traz o vencimento no arquivo e o do layout em que o usuário
/// informa a data.
/// </summary>
public sealed class ImportacaoFaturaParaModeloService
{
    private readonly IUsuarioAtual _usuarioAtual;
    private readonly IUsuarioRepository _usuarios;
    private readonly LeituraFaturaAppService _leituraFaturaAppService;
    private readonly ImportacaoFaturaAppService _importacaoFaturaAppService;
    private readonly PendenciasImportacaoAppService _pendenciasImportacaoAppService;
    private readonly CandidatosPagamentoFaturaAppService _candidatosPagamentoFaturaAppService;
    private readonly ConfirmacaoImportacaoParaModeloService _confirmacaoImportacaoParaModeloService;

    public ImportacaoFaturaParaModeloService(
        IUsuarioAtual usuarioAtual,
        IUsuarioRepository usuarios,
        LeituraFaturaAppService leituraFaturaAppService,
        ImportacaoFaturaAppService importacaoFaturaAppService,
        PendenciasImportacaoAppService pendenciasImportacaoAppService,
        CandidatosPagamentoFaturaAppService candidatosPagamentoFaturaAppService,
        ConfirmacaoImportacaoParaModeloService confirmacaoImportacaoParaModeloService)
    {
        _usuarioAtual = usuarioAtual;
        _usuarios = usuarios;
        _leituraFaturaAppService = leituraFaturaAppService;
        _importacaoFaturaAppService = importacaoFaturaAppService;
        _pendenciasImportacaoAppService = pendenciasImportacaoAppService;
        _candidatosPagamentoFaturaAppService = candidatosPagamentoFaturaAppService;
        _confirmacaoImportacaoParaModeloService = confirmacaoImportacaoParaModeloService;
    }

    /// <summary>A prévia do layout que traz o vencimento no arquivo.</summary>
    /// <param name="arquivo">O arquivo já aberto; quem chama o fecha.</param>
    /// <exception cref="ArgumentException">
    /// Quando o layout não é de fatura ou está em <see cref="LayoutArquivoImportacao.FaturasVencimentoInformado"/>.
    /// </exception>
    public async Task<ImportacaoParaModelo> PreverAsync(Stream arquivo, LayoutArquivoImportacao layout, CancellationToken cancellationToken)
    {
        ExigirVencimentoNoArquivo(layout);
        Result<ImportacaoAplicada<FaturaImportadaDto>, ImportacaoParaModelo> aplicada = await AplicarAsync(arquivo, layout, Maybe<VencimentoFatura>.None, cancellationToken);
        return aplicada.Match(importacao => importacao.Saida, falha => falha);
    }

    /// <summary>A prévia do layout sem o vencimento no arquivo.</summary>
    /// <param name="arquivo">O arquivo já aberto; quem chama o fecha.</param>
    /// <param name="vencimento">A data de vencimento da fatura em <c>yyyy-MM-dd</c>, que o usuário informa.</param>
    /// <exception cref="ArgumentException">Quando o layout não está em <see cref="LayoutArquivoImportacao.FaturasVencimentoInformado"/>.</exception>
    public async Task<ImportacaoParaModelo> PreverVencimentoInformadoAsync(
        Stream arquivo,
        LayoutArquivoImportacao layout,
        string vencimento,
        CancellationToken cancellationToken)
    {
        ExigirVencimentoInformado(layout);
        Result<ImportacaoAplicada<FaturaImportadaDto>, ImportacaoParaModelo> aplicada = await AplicarVencimentoInformadoAsync(arquivo, layout, vencimento, cancellationToken);
        return aplicada.Match(importacao => importacao.Saida, falha => falha);
    }

    /// <summary>A confirmação do layout que traz o vencimento no arquivo.</summary>
    /// <param name="arquivo">O arquivo já aberto, lido de novo; quem chama o fecha.</param>
    /// <param name="resumo">O resumo da prévia que o usuário aprovou, nos argumentos da chamada.</param>
    /// <exception cref="ArgumentException">
    /// Quando o layout não é de fatura ou está em <see cref="LayoutArquivoImportacao.FaturasVencimentoInformado"/>.
    /// </exception>
    public async Task<ImportacaoParaModelo> ConfirmarAsync(
        Stream arquivo,
        LayoutArquivoImportacao layout,
        string resumo,
        CancellationToken cancellationToken)
    {
        ExigirVencimentoNoArquivo(layout);
        Result<ImportacaoAplicada<FaturaImportadaDto>, ImportacaoParaModelo> aplicada = await AplicarAsync(arquivo, layout, Maybe<VencimentoFatura>.None, cancellationToken);
        return await ConfirmarAplicadaAsync(aplicada, layout, resumo, cancellationToken);
    }

    /// <summary>A confirmação do layout sem o vencimento no arquivo.</summary>
    /// <param name="arquivo">O arquivo já aberto, lido de novo; quem chama o fecha.</param>
    /// <param name="vencimento">O vencimento da fatura em <c>yyyy-MM-dd</c>, o mesmo da prévia.</param>
    /// <param name="resumo">O resumo da prévia que o usuário aprovou, nos argumentos da chamada.</param>
    /// <exception cref="ArgumentException">Quando o layout não está em <see cref="LayoutArquivoImportacao.FaturasVencimentoInformado"/>.</exception>
    public async Task<ImportacaoParaModelo> ConfirmarVencimentoInformadoAsync(
        Stream arquivo,
        LayoutArquivoImportacao layout,
        string vencimento,
        string resumo,
        CancellationToken cancellationToken)
    {
        ExigirVencimentoInformado(layout);
        Result<ImportacaoAplicada<FaturaImportadaDto>, ImportacaoParaModelo> aplicada = await AplicarVencimentoInformadoAsync(arquivo, layout, vencimento, cancellationToken);
        return await ConfirmarAplicadaAsync(aplicada, layout, resumo, cancellationToken);
    }

    private Task<ImportacaoParaModelo> ConfirmarAplicadaAsync(
        Result<ImportacaoAplicada<FaturaImportadaDto>, ImportacaoParaModelo> aplicada,
        LayoutArquivoImportacao layout,
        string resumo,
        CancellationToken cancellationToken)
    {
        return _confirmacaoImportacaoParaModeloService.ConfirmarAsync(
            aplicada,
            layout,
            resumo,
            _candidatosPagamentoFaturaAppService.ListarCandidatosFaturaImportadaAsync,
            cancellationToken);
    }

    private async Task<Result<ImportacaoAplicada<FaturaImportadaDto>, ImportacaoParaModelo>> AplicarVencimentoInformadoAsync(
        Stream arquivo,
        LayoutArquivoImportacao layout,
        string vencimento,
        CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParseExact(vencimento, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly data))
            return ImportacaoParaModelo.Falha(ErrosParaModelo.Importacao.VencimentoObrigatorio);

        return await AplicarAsync(arquivo, layout, (VencimentoFatura)data, cancellationToken);
    }

    /// <param name="vencimento">Só no layout sem o vencimento no arquivo, já lido.</param>
    /// <remarks>A falha é a própria saída, que leva as pendências quando o usuário as resolve pela API.</remarks>
    private async Task<Result<ImportacaoAplicada<FaturaImportadaDto>, ImportacaoParaModelo>> AplicarAsync(
        Stream arquivo,
        LayoutArquivoImportacao layout,
        Maybe<VencimentoFatura> vencimento,
        CancellationToken cancellationToken)
    {
        Maybe<Usuario> usuario = await _usuarios.ObterPorIdAsync(_usuarioAtual.Id, cancellationToken);
        if (usuario.HasNoValue)
            return ImportacaoParaModelo.Falha(ErrosParaModelo.Consulta.UsuarioNaoEncontrado);

        Result<Fatura, FalhaLeituraArquivo> lida = await _leituraFaturaAppService.LerAsync(layout, arquivo, vencimento, cancellationToken);
        if (lida.IsFailure)
            return ImportacaoParaModelo.Falha(ErrosParaModelo.Importacao.ArquivoInvalido(lida.Error));

        Fatura fatura = lida.Value;
        Result<FaturaImportadaDto, Erro> importada = await _importacaoFaturaAppService.AplicarAsync(_usuarioAtual.Id, layout, fatura, cancellationToken);
        if (importada.IsSuccess)
            return new ImportacaoAplicada<FaturaImportadaDto>(ImportacaoParaModelo.DaFatura(layout, fatura, importada.Value), importada.Value);

        ErroParaModelo erro = ErrosParaModelo.De(importada.Error);
        if (!ErrosParaModelo.Importacao.PedePendencias(importada.Error))
            return ImportacaoParaModelo.Falha(erro);

        PendenciasFaturaDto pendencias = await _pendenciasImportacaoAppService.ListarDaFaturaAsync(_usuarioAtual.Id, fatura, cancellationToken);
        return ImportacaoParaModelo.Falha(erro, PendenciasImportacaoParaModelo.DaFatura(pendencias));
    }

    /// <summary>A guarda dos métodos sem o vencimento: o layout de outro grupo é erro de programação.</summary>
    private static void ExigirVencimentoNoArquivo(LayoutArquivoImportacao layout)
    {
        if (!LayoutArquivoImportacao.Faturas.Contains(layout) || LayoutArquivoImportacao.FaturasVencimentoInformado.Contains(layout))
            throw new ArgumentException($"O layout {layout.Value} não é de fatura com o vencimento no arquivo.", nameof(layout));
    }

    /// <summary>A guarda dos métodos com o vencimento: o layout de outro grupo é erro de programação.</summary>
    private static void ExigirVencimentoInformado(LayoutArquivoImportacao layout)
    {
        if (!LayoutArquivoImportacao.FaturasVencimentoInformado.Contains(layout))
            throw new ArgumentException($"O layout {layout.Value} não é de fatura com o vencimento informado.", nameof(layout));
    }
}
