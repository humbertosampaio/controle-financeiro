using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using CSharpFunctionalExtensions;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Beneficiarios.RemoverAlias;

/// <summary>
/// Desfaz a associação de um texto de beneficiário do arquivo com o beneficiário do usuário. Para corrigir uma
/// associação, remova o alias e resolva-o de novo. O alias que o beneficiário não tem não muda nada.
/// </summary>
public sealed class RemoverAliasBeneficiarioEndpoint : Endpoint<RemoverAliasBeneficiarioRequest>
{
    private readonly IBeneficiarioRepository _beneficiarioRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoverAliasBeneficiarioEndpoint(IBeneficiarioRepository beneficiarioRepository, IUnitOfWork unitOfWork)
    {
        _beneficiarioRepository = beneficiarioRepository;
        _unitOfWork = unitOfWork;
    }

    public override void Configure()
    {
        Post("usuarios/{idUsuario}/beneficiarios/{idBeneficiario}/aliases/remocao");
        AllowAnonymous();
    }

    // Não carrega o usuário: um usuário que não existe não tem beneficiário, e cai no mesmo 404.
    public override async Task HandleAsync(RemoverAliasBeneficiarioRequest req, CancellationToken ct)
    {
        Maybe<Beneficiario> beneficiario = await _beneficiarioRepository.ObterAsync(IdBeneficiario.From(req.IdBeneficiario), ct);
        if (beneficiario.HasNoValue)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        beneficiario.Value.RemoverAlias((Alias)req.Alias);
        await _unitOfWork.CommitAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
