using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Application.Despesas;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Application.IA.Escritas;

/// <summary>
/// A importação de um extrato de conta que o modelo pede, já como tipo de saída, em duas etapas, como no
/// <see cref="PagamentoFaturaParaModeloService"/>. A regra é a da API, pelo <see cref="ImportacaoExtratoAppService"/>,
/// aplicada em memória nas duas: a prévia nunca grava, e a confirmação, chamada depois que o usuário aprova o resumo no
/// cliente, lê o arquivo de novo e grava pelo <see cref="ConfirmacaoImportacaoParaModeloService"/>. O layout decide de
/// onde vem a conta: do arquivo, ou do sufixo que o usuário informa nos <see cref="ParametrosImportacaoExtratoDto"/>. O
/// serviço recebe o arquivo já aberto e não sabe de onde ele veio.
/// </summary>
public sealed class ImportacaoExtratoParaModeloService
{
    /// <summary>
    /// Como cada layout de extrato obtém o sufixo da conta, pelos grupos do <see cref="LayoutArquivoImportacao"/>: o
    /// informado, nos layouts sem a conta no arquivo, e nenhum, nos que a trazem.
    /// </summary>
    private static readonly Dictionary<LayoutArquivoImportacao, Func<ParametrosImportacaoExtratoDto, Result<Maybe<SufixoConta>, ImportacaoParaModelo>>> _sufixoPorLayout =
        LayoutArquivoImportacao.Extratos
            .Concat(LayoutArquivoImportacao.ExtratosContaInformada)
            .ToDictionary(layout => layout, LeituraSufixo);

    private readonly IUsuarioAtual _usuarioAtual;
    private readonly IUsuarioRepository _usuarios;
    private readonly LeituraExtratoAppService _leituraExtratoAppService;
    private readonly ImportacaoExtratoAppService _importacaoExtratoAppService;
    private readonly PendenciasImportacaoAppService _pendenciasImportacaoAppService;
    private readonly CandidatosPagamentoFaturaAppService _candidatosPagamentoFaturaAppService;
    private readonly ConfirmacaoImportacaoParaModeloService _confirmacaoImportacaoParaModeloService;

    public ImportacaoExtratoParaModeloService(
        IUsuarioAtual usuarioAtual,
        IUsuarioRepository usuarios,
        LeituraExtratoAppService leituraExtratoAppService,
        ImportacaoExtratoAppService importacaoExtratoAppService,
        PendenciasImportacaoAppService pendenciasImportacaoAppService,
        CandidatosPagamentoFaturaAppService candidatosPagamentoFaturaAppService,
        ConfirmacaoImportacaoParaModeloService confirmacaoImportacaoParaModeloService)
    {
        _usuarioAtual = usuarioAtual;
        _usuarios = usuarios;
        _leituraExtratoAppService = leituraExtratoAppService;
        _importacaoExtratoAppService = importacaoExtratoAppService;
        _pendenciasImportacaoAppService = pendenciasImportacaoAppService;
        _candidatosPagamentoFaturaAppService = candidatosPagamentoFaturaAppService;
        _confirmacaoImportacaoParaModeloService = confirmacaoImportacaoParaModeloService;
    }

    /// <param name="arquivo">O arquivo já aberto; quem chama o fecha.</param>
    /// <param name="layout">Um layout de extrato, fixo na ferramenta que chama.</param>
    public async Task<ImportacaoParaModelo> PreverAsync(
        Stream arquivo,
        LayoutArquivoImportacao layout,
        ParametrosImportacaoExtratoDto parametros,
        CancellationToken cancellationToken)
    {
        Result<ImportacaoAplicada<ExtratoImportadoDto>, ImportacaoParaModelo> aplicada = await AplicarAsync(arquivo, layout, parametros, cancellationToken);
        return aplicada.Match(importacao => importacao.Saida, falha => falha);
    }

    /// <param name="arquivo">O arquivo já aberto, lido de novo; quem chama o fecha.</param>
    /// <param name="layout">Um layout de extrato, fixo na ferramenta que chama.</param>
    /// <param name="parametros">Os mesmos da prévia.</param>
    /// <param name="resumo">O resumo da prévia que o usuário aprovou, nos argumentos da chamada.</param>
    public async Task<ImportacaoParaModelo> ConfirmarAsync(
        Stream arquivo,
        LayoutArquivoImportacao layout,
        ParametrosImportacaoExtratoDto parametros,
        string resumo,
        CancellationToken cancellationToken)
    {
        Result<ImportacaoAplicada<ExtratoImportadoDto>, ImportacaoParaModelo> aplicada = await AplicarAsync(arquivo, layout, parametros, cancellationToken);
        return await _confirmacaoImportacaoParaModeloService.ConfirmarAsync(
            aplicada,
            layout,
            resumo,
            _candidatosPagamentoFaturaAppService.ListarCandidatosExtratoAsync,
            cancellationToken);
    }

    /// <remarks>A falha é a própria saída, que leva as pendências quando o usuário as resolve pela API.</remarks>
    private async Task<Result<ImportacaoAplicada<ExtratoImportadoDto>, ImportacaoParaModelo>> AplicarAsync(
        Stream arquivo,
        LayoutArquivoImportacao layout,
        ParametrosImportacaoExtratoDto parametros,
        CancellationToken cancellationToken)
    {
        Result<Maybe<SufixoConta>, ImportacaoParaModelo> sufixo = _sufixoPorLayout[layout](parametros);
        if (sufixo.IsFailure)
            return sufixo.Error;

        Maybe<Usuario> usuario = await _usuarios.ObterPorIdAsync(_usuarioAtual.Id, cancellationToken);
        if (usuario.HasNoValue)
            return ImportacaoParaModelo.Falha(ErrosParaModelo.Consulta.UsuarioNaoEncontrado);

        Result<Extrato, FalhaLeituraArquivo> lido = await _leituraExtratoAppService.LerAsync(layout, arquivo, sufixo.Value, cancellationToken);
        if (lido.IsFailure)
            return ImportacaoParaModelo.Falha(ErrosParaModelo.Importacao.ArquivoInvalido(lido.Error));

        Extrato extrato = lido.Value;
        Result<ExtratoImportadoDto, Erro> importado = await _importacaoExtratoAppService.AplicarAsync(_usuarioAtual.Id, layout, extrato, cancellationToken);
        if (importado.IsSuccess)
            return new ImportacaoAplicada<ExtratoImportadoDto>(ImportacaoParaModelo.DoExtrato(layout, importado.Value), importado.Value);

        ErroParaModelo erro = ErrosParaModelo.De(importado.Error);
        if (!ErrosParaModelo.Importacao.PedePendencias(importado.Error))
            return ImportacaoParaModelo.Falha(erro);

        IReadOnlyList<Alias> beneficiarios = await _pendenciasImportacaoAppService.ListarDoExtratoAsync(_usuarioAtual.Id, extrato, cancellationToken);
        return ImportacaoParaModelo.Falha(erro, PendenciasImportacaoParaModelo.DoExtrato(beneficiarios));
    }

    private static Func<ParametrosImportacaoExtratoDto, Result<Maybe<SufixoConta>, ImportacaoParaModelo>> LeituraSufixo(LayoutArquivoImportacao layout)
    {
        if (LayoutArquivoImportacao.ExtratosContaInformada.Contains(layout))
            return LerSufixoInformado;

        return SemSufixoInformado;
    }

    /// <summary>Nenhum sufixo, no layout que traz a conta no arquivo.</summary>
    private static Result<Maybe<SufixoConta>, ImportacaoParaModelo> SemSufixoInformado(ParametrosImportacaoExtratoDto _) => Maybe<SufixoConta>.None;

    /// <summary>O sufixo que o usuário informou, no layout sem a conta no arquivo.</summary>
    private static Result<Maybe<SufixoConta>, ImportacaoParaModelo> LerSufixoInformado(ParametrosImportacaoExtratoDto parametros)
    {
        Result<SufixoConta> sufixo = SufixoConta.Criar(parametros.SufixoConta);
        if (sufixo.IsFailure)
            return ImportacaoParaModelo.Falha(ErrosParaModelo.Importacao.SufixoContaInvalido);

        return Maybe.From(sufixo.Value);
    }
}
