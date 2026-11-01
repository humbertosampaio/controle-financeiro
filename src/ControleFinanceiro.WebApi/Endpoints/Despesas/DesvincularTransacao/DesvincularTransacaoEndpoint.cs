using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Transacoes;
using CSharpFunctionalExtensions;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Despesas.DesvincularTransacao;

/// <summary>Desfaz o vínculo e devolve a despesa, ou a parcela que a transação quitou, para pendente.</summary>
public sealed class DesvincularTransacaoEndpoint : Endpoint<DesvincularTransacaoRequest>
{
    private readonly IDespesaRepository _despesaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DesvincularTransacaoEndpoint(IDespesaRepository despesaRepository, IUnitOfWork unitOfWork)
    {
        _despesaRepository = despesaRepository;
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
        if (despesa.HasNoValue)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        UnitResult<Erro> desfeito = despesa.Value.DesfazerVinculo(IdTransacao.From(req.IdTransacao));
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
            case ErroTransacaoNaoVinculada:
                await Send.NotFoundAsync(ct);
                break;

            default:
                throw new InvalidOperationException($"Erro sem status mapeado: {erro}");
        }
    }
}
