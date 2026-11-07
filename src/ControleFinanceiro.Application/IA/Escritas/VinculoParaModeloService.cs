using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Transacoes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Application.IA.Escritas;

/// <summary>
/// O vínculo que o modelo pede, já como tipo de saída, em duas etapas. A regra é a do <see cref="VinculoTransacaoService"/>,
/// aplicada em memória nas duas: a prévia nunca grava, e a confirmação, chamada depois que o usuário aprova o resumo no
/// cliente, refaz o vínculo do zero e só grava se o resumo recalculado for o que ele aprovou. Sem gravação, as mudanças em
/// memória morrem com o escopo da requisição. Os repositórios enxergam só o usuário atual, então despesa ou transação de
/// outro usuário é tratada como inexistente.
/// </summary>
public sealed class VinculoParaModeloService
{
    private readonly ITransacaoRepository _transacoes;
    private readonly IDespesaRepository _despesas;
    private readonly IUnitOfWork _unitOfWork;

    public VinculoParaModeloService(ITransacaoRepository transacoes, IDespesaRepository despesas, IUnitOfWork unitOfWork)
    {
        _transacoes = transacoes;
        _despesas = despesas;
        _unitOfWork = unitOfWork;
    }

    /// <param name="idTransacao">Vem do modelo: um id que não é de transação vira erro, e não exceção.</param>
    /// <param name="idDespesa">Vem do modelo: um id que não é de despesa vira erro, e não exceção.</param>
    public async Task<VinculoParaModelo> PreverAsync(Guid idTransacao, Guid idDespesa, CancellationToken cancellationToken)
    {
        return await AplicarAsync(idTransacao, idDespesa, cancellationToken)
            .Match(previa => previa, VinculoParaModelo.Falha);
    }

    /// <param name="idTransacao">Vem do modelo: um id que não é de transação vira erro, e não exceção.</param>
    /// <param name="idDespesa">Vem do modelo: um id que não é de despesa vira erro, e não exceção.</param>
    /// <param name="resumo">O resumo da prévia que o usuário aprovou, nos argumentos da chamada.</param>
    public async Task<VinculoParaModelo> ConfirmarAsync(
        Guid idTransacao,
        Guid idDespesa,
        string resumo,
        CancellationToken cancellationToken)
    {
        return await AplicarAsync(idTransacao, idDespesa, cancellationToken)
            .Ensure(vinculo => vinculo.ConfereResumo(resumo), ErroParaModelo.ResumoDivergente)
            .Tap(() => _unitOfWork.CommitAsync(cancellationToken))
            .Match(vinculo => vinculo, VinculoParaModelo.Falha);
    }

    private async Task<Result<VinculoParaModelo, ErroParaModelo>> AplicarAsync(
        Guid idTransacao,
        Guid idDespesa,
        CancellationToken cancellationToken)
    {
        Maybe<Transacao> transacao = await ObterTransacaoAsync(idTransacao, cancellationToken);
        if (transacao.HasNoValue)
            return ErroParaModelo.TransacaoNaoEncontrada;

        Maybe<Despesa> despesa = await ObterDespesaAsync(idDespesa, cancellationToken);
        if (despesa.HasNoValue)
            return ErroParaModelo.DespesaNaoEncontrada;

        Maybe<Despesa> despesaQueUsaATransacao = await _despesas.ObterPelaTransacaoAsync(transacao.Value.Id, cancellationToken);

        return VinculoTransacaoService
            .Vincular(transacao.Value, despesa.Value, despesaQueUsaATransacao)
            .MapError(ErroParaModelo.De)
            .Map(() => VinculoParaModelo.De(transacao.Value, despesa.Value));
    }

    private async Task<Maybe<Transacao>> ObterTransacaoAsync(Guid idTransacao, CancellationToken cancellationToken)
    {
        if (!IdTransacao.TryFrom(idTransacao, out IdTransacao id))
            return Maybe<Transacao>.None;

        return await _transacoes.ObterAsync(id, cancellationToken);
    }

    private async Task<Maybe<Despesa>> ObterDespesaAsync(Guid idDespesa, CancellationToken cancellationToken)
    {
        if (!IdDespesa.TryFrom(idDespesa, out IdDespesa id))
            return Maybe<Despesa>.None;

        return await _despesas.ObterAsync(id, cancellationToken);
    }
}
