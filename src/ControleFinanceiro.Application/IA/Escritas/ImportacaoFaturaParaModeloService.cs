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
/// aplicada em memória nas duas, e a confirmação grava pelo <see cref="ConfirmacaoImportacaoParaModeloService"/>. O
/// layout decide de onde vem o vencimento: do arquivo, ou da data que o usuário informa nos
/// <see cref="ParametrosImportacaoFaturaDto"/>.
/// </summary>
public sealed class ImportacaoFaturaParaModeloService
{
    /// <summary>
    /// Como cada layout de fatura obtém o vencimento, pelos grupos do <see cref="LayoutArquivoImportacao"/>: o informado,
    /// nos layouts sem o vencimento no arquivo, e nenhum, nos que o trazem.
    /// </summary>
    private static readonly Dictionary<LayoutArquivoImportacao, Func<ParametrosImportacaoFaturaDto, Result<Maybe<VencimentoFatura>, ImportacaoParaModelo>>> _vencimentoPorLayout =
        LayoutArquivoImportacao.Faturas.ToDictionary(layout => layout, LeituraVencimento);

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

    /// <param name="arquivo">O arquivo já aberto; quem chama o fecha.</param>
    /// <param name="layout">Um layout de fatura, fixo na ferramenta que chama.</param>
    public async Task<ImportacaoParaModelo> PreverAsync(
        Stream arquivo,
        LayoutArquivoImportacao layout,
        ParametrosImportacaoFaturaDto parametros,
        CancellationToken cancellationToken)
    {
        Result<ImportacaoAplicada<FaturaImportadaDto>, ImportacaoParaModelo> aplicada = await AplicarAsync(arquivo, layout, parametros, cancellationToken);
        return aplicada.Match(importacao => importacao.Saida, falha => falha);
    }

    /// <param name="arquivo">O arquivo já aberto, lido de novo; quem chama o fecha.</param>
    /// <param name="layout">Um layout de fatura, fixo na ferramenta que chama.</param>
    /// <param name="parametros">Os mesmos da prévia.</param>
    /// <param name="resumo">O resumo da prévia que o usuário aprovou, nos argumentos da chamada.</param>
    public async Task<ImportacaoParaModelo> ConfirmarAsync(
        Stream arquivo,
        LayoutArquivoImportacao layout,
        ParametrosImportacaoFaturaDto parametros,
        string resumo,
        CancellationToken cancellationToken)
    {
        Result<ImportacaoAplicada<FaturaImportadaDto>, ImportacaoParaModelo> aplicada = await AplicarAsync(arquivo, layout, parametros, cancellationToken);
        return await _confirmacaoImportacaoParaModeloService.ConfirmarAsync(
            aplicada,
            layout,
            resumo,
            _candidatosPagamentoFaturaAppService.ListarCandidatosFaturaImportadaAsync,
            cancellationToken);
    }

    /// <remarks>A falha é a própria saída, que leva as pendências quando o usuário as resolve pela API.</remarks>
    private async Task<Result<ImportacaoAplicada<FaturaImportadaDto>, ImportacaoParaModelo>> AplicarAsync(
        Stream arquivo,
        LayoutArquivoImportacao layout,
        ParametrosImportacaoFaturaDto parametros,
        CancellationToken cancellationToken)
    {
        Result<Maybe<VencimentoFatura>, ImportacaoParaModelo> vencimento = _vencimentoPorLayout[layout](parametros);
        if (vencimento.IsFailure)
            return vencimento.Error;

        Maybe<Usuario> usuario = await _usuarios.ObterPorIdAsync(_usuarioAtual.Id, cancellationToken);
        if (usuario.HasNoValue)
            return ImportacaoParaModelo.Falha(ErrosParaModelo.Consulta.UsuarioNaoEncontrado);

        Result<Fatura, FalhaLeituraArquivo> lida = await _leituraFaturaAppService.LerAsync(layout, arquivo, vencimento.Value, cancellationToken);
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

    private static Func<ParametrosImportacaoFaturaDto, Result<Maybe<VencimentoFatura>, ImportacaoParaModelo>> LeituraVencimento(LayoutArquivoImportacao layout)
    {
        if (LayoutArquivoImportacao.FaturasVencimentoInformado.Contains(layout))
            return LerVencimentoInformado;

        return SemVencimentoInformado;
    }

    /// <summary>Nenhum vencimento, no layout que traz o vencimento no arquivo.</summary>
    private static Result<Maybe<VencimentoFatura>, ImportacaoParaModelo> SemVencimentoInformado(ParametrosImportacaoFaturaDto _) => Maybe<VencimentoFatura>.None;

    /// <summary>O vencimento que o usuário informou em <c>yyyy-MM-dd</c>, no layout sem o vencimento no arquivo.</summary>
    private static Result<Maybe<VencimentoFatura>, ImportacaoParaModelo> LerVencimentoInformado(ParametrosImportacaoFaturaDto parametros)
    {
        if (!DateOnly.TryParseExact(parametros.Vencimento, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly data))
            return ImportacaoParaModelo.Falha(ErrosParaModelo.Importacao.VencimentoObrigatorio);

        return Maybe.From((VencimentoFatura)data);
    }
}
