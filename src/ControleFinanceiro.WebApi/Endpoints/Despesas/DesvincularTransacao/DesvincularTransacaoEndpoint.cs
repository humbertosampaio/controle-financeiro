using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Transacoes;
using CSharpFunctionalExtensions;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Despesas.DesvincularTransacao;

/// <summary>
/// Desfaz o que a transação fez na despesa: a compra do cartão perde a cobrança, e a saída da conta deixa de pagar a
/// despesa simples, que volta para pendente.
/// </summary>
public sealed class DesvincularTransacaoEndpoint : Endpoint<DesvincularTransacaoRequest>
{
    private readonly IDespesaRepository _despesaRepository;
    private readonly ITransacaoRepository _transacaoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DesvincularTransacaoEndpoint(
        IDespesaRepository despesaRepository,
        ITransacaoRepository transacaoRepository,
        IUnitOfWork unitOfWork)
    {
        _despesaRepository = despesaRepository;
        _transacaoRepository = transacaoRepository;
        _unitOfWork = unitOfWork;
    }

    public override void Configure()
    {
        Delete("usuarios/{idUsuario}/despesas/{idDespesa}/vinculos/{idTransacao}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(DesvincularTransacaoRequest req, CancellationToken ct)
    {
        Maybe<Despesa> despesa = await _despesaRepository.ObterAsync(IdDespesa.From(req.IdDespesa), ct);
        Maybe<Transacao> transacao = await _transacaoRepository.ObterAsync(IdTransacao.From(req.IdTransacao), ct);
        if (despesa.HasNoValue || transacao.HasNoValue)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        UnitResult<Erro> desfeito = VinculoTransacaoService.Desvincular(transacao.Value, despesa.Value);
        if (desfeito.IsFailure)
        {
            await EnviarErroAsync(desfeito.Error, ct);
            return;
        }

        await _unitOfWork.CommitAsync(ct);
        await Send.NoContentAsync(ct);
    }

    private async Task EnviarErroAsync(Erro erro, CancellationToken ct)
    {
        switch (erro)
        {
            case ErroTransacaoNaoCobrouADespesa or ErroTransacaoNaoPagouADespesa:
                await Send.NotFoundAsync(ct);
                break;

            default:
                throw new InvalidOperationException($"Erro sem status mapeado: {erro}");
        }
    }
}
