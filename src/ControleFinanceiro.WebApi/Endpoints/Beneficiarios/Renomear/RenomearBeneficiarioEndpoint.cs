using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using CSharpFunctionalExtensions;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Beneficiarios.Renomear;

/// <summary>
/// Troca o nome de um beneficiário do usuário. Os aliases ficam, e o nome pode repetir o de outro beneficiário: a busca
/// casa só com os aliases.
/// </summary>
public sealed class RenomearBeneficiarioEndpoint : Endpoint<RenomearBeneficiarioRequest>
{
    private readonly IBeneficiarioRepository _beneficiarioRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RenomearBeneficiarioEndpoint(IBeneficiarioRepository beneficiarioRepository, IUnitOfWork unitOfWork)
    {
        _beneficiarioRepository = beneficiarioRepository;
        _unitOfWork = unitOfWork;
    }

    public override void Configure()
    {
        Put("usuarios/{idUsuario}/beneficiarios/{idBeneficiario}/nome");
        AllowAnonymous();
    }

    public override async Task HandleAsync(RenomearBeneficiarioRequest req, CancellationToken ct)
    {
        Maybe<Beneficiario> beneficiario = await _beneficiarioRepository.ObterAsync(IdBeneficiario.From(req.IdBeneficiario), ct);
        if (beneficiario.HasNoValue)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        beneficiario.Value.Renomear((TextoObrigatorio)req.Nome);
        await _unitOfWork.CommitAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
