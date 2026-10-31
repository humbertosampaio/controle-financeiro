using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Cartoes;
using CSharpFunctionalExtensions;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Usuarios.AlterarCicloFaturaCartao;

/// <summary>
/// Troca os dias de vencimento e de fechamento de um cartão do usuário. As despesas já geradas não mudam: os dias novos
/// valem para as geradas depois.
/// </summary>
public sealed class AlterarCicloFaturaCartaoEndpoint : Endpoint<AlterarCicloFaturaCartaoRequest>
{
    private readonly ICartaoRepository _cartaoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AlterarCicloFaturaCartaoEndpoint(ICartaoRepository cartaoRepository, IUnitOfWork unitOfWork)
    {
        _cartaoRepository = cartaoRepository;
        _unitOfWork = unitOfWork;
    }

    public override void Configure()
    {
        Put("usuarios/{idUsuario}/cartoes/{idCartao}/ciclo-fatura");
        AllowAnonymous();
    }

    public override async Task HandleAsync(AlterarCicloFaturaCartaoRequest req, CancellationToken ct)
    {
        Maybe<Cartao> cartao = await _cartaoRepository.ObterAsync(IdCartao.From(req.IdCartao), ct);

        if (cartao.HasNoValue)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        cartao.Value.AlterarCicloFatura((DiaVencimentoCartao)req.DiaVencimento, (DiaFechamentoCartao)req.DiaFechamento);
        await _unitOfWork.CommitAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
