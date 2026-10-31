using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using CSharpFunctionalExtensions;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Despesas.AlterarCategoria;

/// <summary>
/// Troca a categoria de uma despesa do usuário, de qualquer tipo. Não deixa a despesa sem categoria: para deixar uma
/// categoria de lado, usa-se a Outros.
/// </summary>
public sealed class AlterarCategoriaDespesaEndpoint : Endpoint<AlterarCategoriaDespesaRequest>
{
    private readonly IDespesaRepository _despesaRepository;
    private readonly ICategoriaDespesaRepository _categoriaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AlterarCategoriaDespesaEndpoint(
        IDespesaRepository despesaRepository,
        ICategoriaDespesaRepository categoriaRepository,
        IUnitOfWork unitOfWork)
    {
        _despesaRepository = despesaRepository;
        _categoriaRepository = categoriaRepository;
        _unitOfWork = unitOfWork;
    }

    public override void Configure()
    {
        Put("usuarios/{idUsuario}/despesas/{idDespesa}/categoria");
        AllowAnonymous();
    }

    public override async Task HandleAsync(AlterarCategoriaDespesaRequest req, CancellationToken ct)
    {
        Maybe<Despesa> despesa = await _despesaRepository.ObterAsync(IdDespesa.From(req.IdDespesa), ct);
        Maybe<CategoriaDespesa> categoria = await _categoriaRepository.ObterAsync(IdCategoriaDespesa.From(req.IdCategoria), ct);
        if (despesa.HasNoValue || categoria.HasNoValue)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        AlteracaoCategoriaDespesaService.Alterar(despesa.Value, categoria.Value);
        await _unitOfWork.CommitAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
