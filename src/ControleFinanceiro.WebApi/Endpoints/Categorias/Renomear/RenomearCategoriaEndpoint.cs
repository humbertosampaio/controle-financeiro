using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using CSharpFunctionalExtensions;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Categorias.Renomear;

/// <summary>Troca o nome de uma categoria do usuário. Os aliases ficam, e a categoria Outros não é renomeada.</summary>
public sealed class RenomearCategoriaEndpoint : Endpoint<RenomearCategoriaRequest>
{
    private readonly ICategoriaDespesaRepository _categoriaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RenomearCategoriaEndpoint(ICategoriaDespesaRepository categoriaRepository, IUnitOfWork unitOfWork)
    {
        _categoriaRepository = categoriaRepository;
        _unitOfWork = unitOfWork;
    }

    public override void Configure()
    {
        Put("usuarios/{idUsuario}/categorias/{idCategoria}/nome");
        AllowAnonymous();
    }

    public override async Task HandleAsync(RenomearCategoriaRequest req, CancellationToken ct)
    {
        Maybe<CategoriaDespesa> categoria = await _categoriaRepository.ObterAsync(IdCategoriaDespesa.From(req.IdCategoria), ct);
        if (categoria.HasNoValue)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        IReadOnlyList<CategoriaDespesa> categoriasDoUsuario = await _categoriaRepository.ListarAsync(ct);
        UnitResult<Erro> renomeada = CategoriaDespesaFactory.Renomear(
            categoriasDoUsuario,
            categoria.Value,
            (TextoObrigatorio)req.Nome);

        await renomeada
            .Match(
                onSuccess: async () =>
                {
                    await _unitOfWork.CommitAsync(ct);
                    await Send.NoContentAsync(ct);
                },
                onFailure: async (Erro erro) =>
                {
                    await EnviarErroAsync(erro, ct);
                });
    }

    private async Task EnviarErroAsync(Erro erro, CancellationToken ct)
    {
        switch (erro)
        {
            case ErroCategoriaJaCadastrada or ErroCategoriaOutrosNaoRenomeavel:
                AddError(r => r.Nome, erro.Descricao);
                await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
                break;

            default:
                throw new InvalidOperationException($"Erro sem status mapeado: {erro}");
        }
    }
}
