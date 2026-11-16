using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Application.Despesas;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Domain.Importacoes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Application.IA.Escritas;

/// <summary>
/// A etapa comum da confirmação da importação de um extrato ou de uma fatura, depois que o
/// <see cref="ImportacaoExtratoParaModeloService"/> ou o <see cref="ImportacaoFaturaParaModeloService"/> a aplicou em
/// memória: só grava se o resumo recalculado for o que o usuário aprovou, e depois de gravar traz os candidatos a
/// pagamento de fatura que a importação revelou.
/// </summary>
public sealed class ConfirmacaoImportacaoParaModeloService
{
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmacaoImportacaoParaModeloService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <param name="aplicada">A importação aplicada em memória, ou a saída da falha, devolvida como está.</param>
    /// <param name="resumo">O resumo da prévia que o usuário aprovou, nos argumentos da chamada.</param>
    /// <param name="listarCandidatos">
    /// A consulta dos candidatos pelo relato da importação, que só enxerga o que foi gravado.
    /// </param>
    public async Task<ImportacaoParaModelo> ConfirmarAsync<TRelato>(
        Result<ImportacaoAplicada<TRelato>, ImportacaoParaModelo> aplicada,
        LayoutArquivoImportacao layout,
        string resumo,
        Func<TRelato, CancellationToken, Task<IReadOnlyList<CandidatoPagamentoFaturaDto>>> listarCandidatos,
        CancellationToken cancellationToken)
    {
        if (aplicada.IsFailure)
            return aplicada.Error;

        (ImportacaoParaModelo saida, TRelato relato) = aplicada.Value;
        if (!saida.ConfereResumo(resumo))
            return ImportacaoParaModelo.Falha(ErrosParaModelo.Importacao.ResumoDivergente(layout));

        await _unitOfWork.CommitAsync(cancellationToken);

        IReadOnlyList<CandidatoPagamentoFaturaDto> candidatos = await listarCandidatos(relato, cancellationToken);
        return saida.ComCandidatos(candidatos.Select(candidatoDto => candidatoDto.ParaModelo()).ToArray());
    }
}

/// <summary>
/// A importação aplicada em memória sem erro: a saída para o modelo e o relato do extrato ou da fatura, de que a
/// confirmação tira os candidatos depois de gravar.
/// </summary>
public sealed record ImportacaoAplicada<TRelato>(ImportacaoParaModelo Saida, TRelato Relato);
