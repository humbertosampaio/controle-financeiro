using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using CSharpFunctionalExtensions;
using System.Globalization;

namespace ControleFinanceiro.Application.IA.Escritas;

/// <summary>
/// A importação de um extrato ou de uma fatura que o modelo pede, já como tipo de saída, em duas etapas, como no
/// <see cref="PagamentoFaturaParaModeloService"/>. A regra é a da API, pelo <see cref="ImportacaoExtratoService"/> e pelo
/// <see cref="ImportacaoFaturaService"/>, aplicada em memória nas duas: a prévia nunca grava, e a confirmação, chamada
/// depois que o usuário aprova o resumo no cliente, lê o arquivo de novo e só grava se o resumo recalculado for o que
/// ele aprovou. O serviço recebe o arquivo já aberto e não sabe de onde ele veio.
/// </summary>
public sealed class ImportacaoParaModeloService
{
    private readonly IUsuarioAtual _usuarioAtual;
    private readonly IUsuarioRepository _usuarios;
    private readonly LeituraExtratoService _leituraExtrato;
    private readonly LeituraFaturaService _leituraFatura;
    private readonly ImportacaoExtratoService _importacaoExtrato;
    private readonly ImportacaoFaturaService _importacaoFatura;
    private readonly PendenciasImportacaoService _pendencias;
    private readonly IUnitOfWork _unitOfWork;

    public ImportacaoParaModeloService(
        IUsuarioAtual usuarioAtual,
        IUsuarioRepository usuarios,
        LeituraExtratoService leituraExtrato,
        LeituraFaturaService leituraFatura,
        ImportacaoExtratoService importacaoExtrato,
        ImportacaoFaturaService importacaoFatura,
        PendenciasImportacaoService pendencias,
        IUnitOfWork unitOfWork)
    {
        _usuarioAtual = usuarioAtual;
        _usuarios = usuarios;
        _leituraExtrato = leituraExtrato;
        _leituraFatura = leituraFatura;
        _importacaoExtrato = importacaoExtrato;
        _importacaoFatura = importacaoFatura;
        _pendencias = pendencias;
        _unitOfWork = unitOfWork;
    }

    /// <param name="arquivo">O arquivo já aberto; quem chama o fecha.</param>
    /// <param name="layout">Vem do modelo: um código que não é de layout vira erro, e não exceção.</param>
    /// <param name="sufixoConta">Lido só no layout de extrato sem a conta no arquivo, em que é obrigatório.</param>
    /// <param name="vencimento">
    /// A data de vencimento da fatura em <c>yyyy-MM-dd</c>, obrigatória no layout de fatura sem o vencimento no arquivo e
    /// recusada no layout que o traz.
    /// </param>
    public Task<ImportacaoParaModelo> PreverAsync(
        Stream arquivo,
        string layout,
        string? sufixoConta,
        string? vencimento,
        CancellationToken cancellationToken)
    {
        return AplicarAsync(arquivo, layout, sufixoConta, vencimento, cancellationToken);
    }

    /// <param name="arquivo">O arquivo já aberto, lido de novo; quem chama o fecha.</param>
    /// <param name="layout">Vem do modelo: um código que não é de layout vira erro, e não exceção.</param>
    /// <param name="sufixoConta">Lido só no layout de extrato sem a conta no arquivo, em que é obrigatório.</param>
    /// <param name="vencimento">A data de vencimento da fatura, pela regra do <see cref="PreverAsync"/>.</param>
    /// <param name="resumo">O resumo da prévia que o usuário aprovou, nos argumentos da chamada.</param>
    public async Task<ImportacaoParaModelo> ConfirmarAsync(
        Stream arquivo,
        string layout,
        string? sufixoConta,
        string? vencimento,
        string resumo,
        CancellationToken cancellationToken)
    {
        ImportacaoParaModelo importacao = await AplicarAsync(arquivo, layout, sufixoConta, vencimento, cancellationToken);
        if (importacao.Erro is not null)
            return importacao;

        if (!importacao.ConfereResumo(resumo))
            return ImportacaoParaModelo.Falha(ErrosParaModelo.Importacao.ResumoDivergente);

        await _unitOfWork.CommitAsync(cancellationToken);
        return importacao;
    }

    /// <remarks>
    /// Devolve a própria saída, e não um <see cref="Result"/>: ela já é o sucesso ou a falha, que leva as pendências
    /// quando o usuário as resolve pela API.
    /// </remarks>
    private async Task<ImportacaoParaModelo> AplicarAsync(
        Stream arquivo,
        string codigoLayout,
        string? sufixoConta,
        string? vencimento,
        CancellationToken cancellationToken)
    {
        if (!LayoutArquivoImportacao.TryFromValue(codigoLayout, out LayoutArquivoImportacao? layout))
            return ImportacaoParaModelo.Falha(ErrosParaModelo.Importacao.LayoutInvalido);

        Maybe<Usuario> usuario = await _usuarios.ObterPorIdAsync(_usuarioAtual.Id, cancellationToken);
        if (usuario.HasNoValue)
            return ImportacaoParaModelo.Falha(ErrosParaModelo.Consulta.UsuarioNaoEncontrado);

        return LayoutArquivoImportacao.Faturas.Contains(layout)
            ? await AplicarFaturaAsync(arquivo, layout, vencimento, cancellationToken)
            : await AplicarExtratoAsync(arquivo, layout, sufixoConta, cancellationToken);
    }

    private async Task<ImportacaoParaModelo> AplicarExtratoAsync(
        Stream arquivo,
        LayoutArquivoImportacao layout,
        string? sufixoConta,
        CancellationToken cancellationToken)
    {
        Maybe<SufixoConta> sufixo = Maybe<SufixoConta>.None;
        if (LayoutArquivoImportacao.ExtratosContaInformada.Contains(layout))
        {
            Result<SufixoConta> informado = SufixoConta.Criar(sufixoConta);
            if (informado.IsFailure)
                return ImportacaoParaModelo.Falha(ErrosParaModelo.Importacao.SufixoContaInvalido);

            sufixo = informado.Value;
        }

        Result<Extrato, FalhaLeituraArquivo> lido = await _leituraExtrato.LerAsync(layout, arquivo, sufixo, cancellationToken);
        if (lido.IsFailure)
            return ImportacaoParaModelo.Falha(ErrosParaModelo.Importacao.ArquivoInvalido(lido.Error));

        Extrato extrato = lido.Value;
        Result<ExtratoImportado, Erro> importado = await _importacaoExtrato.AplicarAsync(_usuarioAtual.Id, layout, extrato, cancellationToken);
        if (importado.IsSuccess)
            return ImportacaoParaModelo.DoExtrato(layout, importado.Value);

        ErroParaModelo erro = ErrosParaModelo.De(importado.Error);
        if (!PedePendencias(importado.Error))
            return ImportacaoParaModelo.Falha(erro);

        IReadOnlyList<Alias> beneficiarios = await _pendencias.ListarDoExtratoAsync(_usuarioAtual.Id, extrato, cancellationToken);
        return ImportacaoParaModelo.Falha(erro, PendenciasImportacaoParaModelo.DoExtrato(beneficiarios));
    }

    private async Task<ImportacaoParaModelo> AplicarFaturaAsync(
        Stream arquivo,
        LayoutArquivoImportacao layout,
        string? vencimento,
        CancellationToken cancellationToken)
    {
        Result<Maybe<DateOnly>, ErroParaModelo> vencimentoLayout = LerVencimento(layout, vencimento);
        if (vencimentoLayout.IsFailure)
            return ImportacaoParaModelo.Falha(vencimentoLayout.Error);

        Result<Fatura, FalhaLeituraArquivo> lida = await _leituraFatura.LerAsync(layout, arquivo, vencimentoLayout.Value, cancellationToken);
        if (lida.IsFailure)
            return ImportacaoParaModelo.Falha(ErrosParaModelo.Importacao.ArquivoInvalido(lida.Error));

        Fatura fatura = lida.Value;
        Result<FaturaImportada, Erro> importada = await _importacaoFatura.AplicarAsync(_usuarioAtual.Id, layout, fatura, cancellationToken);
        if (importada.IsSuccess)
            return ImportacaoParaModelo.DaFatura(layout, fatura, importada.Value);

        ErroParaModelo erro = ErrosParaModelo.De(importada.Error);
        if (!PedePendencias(importada.Error))
            return ImportacaoParaModelo.Falha(erro);

        PendenciasFatura pendencias = await _pendencias.ListarDaFaturaAsync(_usuarioAtual.Id, fatura, cancellationToken);
        return ImportacaoParaModelo.Falha(erro, PendenciasImportacaoParaModelo.DaFatura(pendencias));
    }

    /// <summary>
    /// O vencimento que o usuário informou, só no layout de fatura sem o vencimento no arquivo. Vazio vale como não
    /// informado.
    /// </summary>
    private static Result<Maybe<DateOnly>, ErroParaModelo> LerVencimento(LayoutArquivoImportacao layout, string? vencimento)
    {
        bool informado = !string.IsNullOrWhiteSpace(vencimento);
        if (!LayoutArquivoImportacao.FaturasVencimentoInformado.Contains(layout))
        {
            return informado
                ? ErrosParaModelo.Importacao.VencimentoNoArquivo
                : Maybe<DateOnly>.None;
        }

        return DateOnly.TryParseExact(vencimento, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly data)
            ? Maybe.From(data)
            : ErrosParaModelo.Importacao.VencimentoObrigatorio;
    }

    /// <summary>Os erros que o usuário resolve cadastrando o que falta pela API.</summary>
    private static bool PedePendencias(Erro erro) =>
        erro is ErroCartoesNaoResolvidos or ErroBeneficiariosNaoResolvidos or ErroCategoriasBancoNaoResolvidas;
}
