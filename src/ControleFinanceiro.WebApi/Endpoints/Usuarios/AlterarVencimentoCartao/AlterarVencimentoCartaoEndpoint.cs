using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Cartoes;
using CSharpFunctionalExtensions;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Usuarios.AlterarVencimentoCartao;

/// <summary>
/// Troca o dia de vencimento de um cartão do usuário. As despesas já geradas não mudam: o dia novo vale para as
/// geradas depois.
/// </summary>
public sealed class AlterarVencimentoCartaoEndpoint : Endpoint<AlterarVencimentoCartaoRequest>
{
    private readonly ICartaoRepository _cartaoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AlterarVencimentoCartaoEndpoint(ICartaoRepository cartaoRepository, IUnitOfWork unitOfWork)
    {
        _cartaoRepository = cartaoRepository;
        _unitOfWork = unitOfWork;
    }

    public override void Configure()
    {
        Put("usuarios/{idUsuario}/cartoes/{idCartao}/vencimento");
        AllowAnonymous();
    }

    // Não carrega o usuário: um usuário que não existe não tem cartão, e cai no mesmo 404.
    public override async Task HandleAsync(AlterarVencimentoCartaoRequest req, CancellationToken ct)
    {
        Maybe<Cartao> cartao = await _cartaoRepository.ObterAsync(IdCartao.From(req.IdCartao), ct);

        if (cartao.HasNoValue)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        cartao.Value.AlterarVencimento((DiaVencimentoCartao)req.DiaVencimento);
        await _unitOfWork.CommitAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
