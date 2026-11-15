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
/// cliente, lê o arquivo de novo e grava pelo <see cref="ConfirmacaoImportacaoParaModeloService"/>. Um par de métodos
/// por tipo de entrada: o do layout que traz a conta no arquivo e o do layout em que o usuário informa o sufixo. O
/// serviço recebe o arquivo já aberto e não sabe de onde ele veio.
/// </summary>
public sealed class ImportacaoExtratoParaModeloService
{
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

    /// <summary>A prévia do layout que traz a conta no arquivo.</summary>
    /// <param name="arquivo">O arquivo já aberto; quem chama o fecha.</param>
    /// <exception cref="ArgumentException">Quando o layout não está em <see cref="LayoutArquivoImportacao.Extratos"/>.</exception>
    public async Task<ImportacaoParaModelo> PreverAsync(Stream arquivo, LayoutArquivoImportacao layout, CancellationToken cancellationToken)
    {
        ExigirLayoutEm(LayoutArquivoImportacao.Extratos, layout);
        Result<ImportacaoAplicada<ExtratoImportadoDto>, ImportacaoParaModelo> aplicada = await AplicarAsync(arquivo, layout, Maybe<SufixoConta>.None, cancellationToken);
        return aplicada.Match(importacao => importacao.Saida, falha => falha);
    }

    /// <summary>A prévia do layout sem a conta no arquivo.</summary>
    /// <param name="arquivo">O arquivo já aberto; quem chama o fecha.</param>
    /// <param name="sufixoConta">Os 4 últimos caracteres do número da conta, que o usuário informa.</param>
    /// <exception cref="ArgumentException">Quando o layout não está em <see cref="LayoutArquivoImportacao.ExtratosContaInformada"/>.</exception>
    public async Task<ImportacaoParaModelo> PreverContaInformadaAsync(
        Stream arquivo,
        LayoutArquivoImportacao layout,
        string sufixoConta,
        CancellationToken cancellationToken)
    {
        ExigirLayoutEm(LayoutArquivoImportacao.ExtratosContaInformada, layout);
        Result<ImportacaoAplicada<ExtratoImportadoDto>, ImportacaoParaModelo> aplicada = await AplicarContaInformadaAsync(arquivo, layout, sufixoConta, cancellationToken);
        return aplicada.Match(importacao => importacao.Saida, falha => falha);
    }

    /// <summary>A confirmação do layout que traz a conta no arquivo.</summary>
    /// <param name="arquivo">O arquivo já aberto, lido de novo; quem chama o fecha.</param>
    /// <param name="resumo">O resumo da prévia que o usuário aprovou, nos argumentos da chamada.</param>
    /// <exception cref="ArgumentException">Quando o layout não está em <see cref="LayoutArquivoImportacao.Extratos"/>.</exception>
    public async Task<ImportacaoParaModelo> ConfirmarAsync(
        Stream arquivo,
        LayoutArquivoImportacao layout,
        string resumo,
        CancellationToken cancellationToken)
    {
        ExigirLayoutEm(LayoutArquivoImportacao.Extratos, layout);
        Result<ImportacaoAplicada<ExtratoImportadoDto>, ImportacaoParaModelo> aplicada = await AplicarAsync(arquivo, layout, Maybe<SufixoConta>.None, cancellationToken);
        return await ConfirmarAplicadaAsync(aplicada, layout, resumo, cancellationToken);
    }

    /// <summary>A confirmação do layout sem a conta no arquivo.</summary>
    /// <param name="arquivo">O arquivo já aberto, lido de novo; quem chama o fecha.</param>
    /// <param name="sufixoConta">O sufixo da conta, o mesmo da prévia.</param>
    /// <param name="resumo">O resumo da prévia que o usuário aprovou, nos argumentos da chamada.</param>
    /// <exception cref="ArgumentException">Quando o layout não está em <see cref="LayoutArquivoImportacao.ExtratosContaInformada"/>.</exception>
    public async Task<ImportacaoParaModelo> ConfirmarContaInformadaAsync(
        Stream arquivo,
        LayoutArquivoImportacao layout,
        string sufixoConta,
        string resumo,
        CancellationToken cancellationToken)
    {
        ExigirLayoutEm(LayoutArquivoImportacao.ExtratosContaInformada, layout);
        Result<ImportacaoAplicada<ExtratoImportadoDto>, ImportacaoParaModelo> aplicada = await AplicarContaInformadaAsync(arquivo, layout, sufixoConta, cancellationToken);
        return await ConfirmarAplicadaAsync(aplicada, layout, resumo, cancellationToken);
    }

    private Task<ImportacaoParaModelo> ConfirmarAplicadaAsync(
        Result<ImportacaoAplicada<ExtratoImportadoDto>, ImportacaoParaModelo> aplicada,
        LayoutArquivoImportacao layout,
        string resumo,
        CancellationToken cancellationToken)
    {
        return _confirmacaoImportacaoParaModeloService.ConfirmarAsync(
            aplicada,
            layout,
            resumo,
            _candidatosPagamentoFaturaAppService.ListarCandidatosExtratoAsync,
            cancellationToken);
    }

    private async Task<Result<ImportacaoAplicada<ExtratoImportadoDto>, ImportacaoParaModelo>> AplicarContaInformadaAsync(
        Stream arquivo,
        LayoutArquivoImportacao layout,
        string sufixoConta,
        CancellationToken cancellationToken)
    {
        Result<SufixoConta> sufixo = SufixoConta.Criar(sufixoConta);
        if (sufixo.IsFailure)
            return ImportacaoParaModelo.Falha(ErrosParaModelo.Importacao.SufixoContaInvalido);

        return await AplicarAsync(arquivo, layout, sufixo.Value, cancellationToken);
    }

    /// <param name="sufixo">Só no layout sem a conta no arquivo, já lido.</param>
    /// <remarks>A falha é a própria saída, que leva as pendências quando o usuário as resolve pela API.</remarks>
    private async Task<Result<ImportacaoAplicada<ExtratoImportadoDto>, ImportacaoParaModelo>> AplicarAsync(
        Stream arquivo,
        LayoutArquivoImportacao layout,
        Maybe<SufixoConta> sufixo,
        CancellationToken cancellationToken)
    {
        Maybe<Usuario> usuario = await _usuarios.ObterPorIdAsync(_usuarioAtual.Id, cancellationToken);
        if (usuario.HasNoValue)
            return ImportacaoParaModelo.Falha(ErrosParaModelo.Consulta.UsuarioNaoEncontrado);

        Result<Extrato, FalhaLeituraArquivo> lido = await _leituraExtratoAppService.LerAsync(layout, arquivo, sufixo, cancellationToken);
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

    /// <summary>A guarda dos métodos por tipo de entrada: o layout de outro grupo é erro de programação.</summary>
    private static void ExigirLayoutEm(IReadOnlyCollection<LayoutArquivoImportacao> grupo, LayoutArquivoImportacao layout)
    {
        if (!grupo.Contains(layout))
            throw new ArgumentException($"O layout {layout.Value} não é deste método da importação do extrato.", nameof(layout));
    }
}
