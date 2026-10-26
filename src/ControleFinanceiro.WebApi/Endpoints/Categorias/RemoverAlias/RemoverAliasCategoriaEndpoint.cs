using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using CSharpFunctionalExtensions;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Categorias.RemoverAlias;

/// <summary>
/// Desfaz a associação de uma categoria do banco com a categoria do usuário. Para corrigir uma associação, remova o
/// alias e resolva-o de novo. O alias que a categoria não tem não muda nada.
/// </summary>
public sealed class RemoverAliasCategoriaEndpoint : Endpoint<RemoverAliasCategoriaRequest>
{
    private readonly ICategoriaDespesaRepository _categoriaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoverAliasCategoriaEndpoint(ICategoriaDespesaRepository categoriaRepository, IUnitOfWork unitOfWork)
    {
        _categoriaRepository = categoriaRepository;
        _unitOfWork = unitOfWork;
    }

    public override void Configure()
    {
        Post("usuarios/{idUsuario}/categorias/{idCategoria}/aliases/remocao");
        AllowAnonymous();
    }

    // Não carrega o usuário: um usuário que não existe não tem categoria, e cai no mesmo 404.
    public override async Task HandleAsync(RemoverAliasCategoriaRequest req, CancellationToken ct)
    {
        Maybe<CategoriaDespesa> categoria = await _categoriaRepository.ObterAsync(IdCategoriaDespesa.From(req.IdCategoria), ct);
        if (categoria.HasNoValue)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        categoria.Value.RemoverAlias((Alias)req.Alias);
        await _unitOfWork.CommitAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
