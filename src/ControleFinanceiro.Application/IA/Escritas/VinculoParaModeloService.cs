using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Transacoes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Application.IA.Escritas;

/// <summary>
/// O vínculo que o modelo pede, já como tipo de saída. A regra é a do <see cref="VinculoPagamentoService"/>, aplicada em
/// memória; só o vínculo válido vai ao usuário, e só o confirmado é gravado. Sem confirmação, as mudanças em memória
/// morrem com o escopo da requisição. Os repositórios enxergam só o usuário atual, então despesa ou transação de outro
/// usuário é tratada como inexistente.
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
    /// <param name="confirmacao">Quem pergunta ao usuário, ligado à requisição do modelo.</param>
    public async Task<VinculoParaModelo> VincularAsync(
        Guid idTransacao,
        Guid idDespesa,
        IConfirmacaoHumana confirmacao,
        CancellationToken cancellationToken)
    {
        Maybe<Transacao> transacao = await ObterTransacaoAsync(idTransacao, cancellationToken);
        if (transacao.HasNoValue)
            return VinculoParaModelo.Falha(ErroParaModelo.TransacaoNaoEncontrada);

        Maybe<Despesa> despesa = await ObterDespesaAsync(idDespesa, cancellationToken);
        if (despesa.HasNoValue)
            return VinculoParaModelo.Falha(ErroParaModelo.DespesaNaoEncontrada);

        Maybe<Despesa> despesaQueUsaATransacao = await _despesas.ObterPelaTransacaoAsync(transacao.Value.Id, cancellationToken);

        UnitResult<Erro> vinculo = VinculoPagamentoService.Vincular(transacao.Value, despesa.Value, despesaQueUsaATransacao);
        if (vinculo.IsFailure)
            return VinculoParaModelo.Falha(ErroParaModelo.De(vinculo.Error));

        VinculoParaModelo previa = VinculoParaModelo.De(transacao.Value, despesa.Value);

        RespostaConfirmacao resposta = await confirmacao.PedirAsync(previa, cancellationToken);
        if (resposta == RespostaConfirmacao.Recusada)
            return VinculoParaModelo.Falha(ErroParaModelo.ConfirmacaoRecusada);

        if (resposta != RespostaConfirmacao.Confirmada)
            return VinculoParaModelo.Falha(ErroParaModelo.ConfirmacaoIndisponivel);

        await _unitOfWork.CommitAsync(cancellationToken);
        return previa;
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
