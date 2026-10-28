using ControleFinanceiro.Domain.Categorias;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Categorias.Listar;

/// <summary>As categorias do usuário com os aliases de cada uma, para a tela da resolução e a do cadastro.</summary>
public sealed class ListarCategoriasEndpoint : Endpoint<ListarCategoriasRequest, ListarCategoriasResponse>
{
    private readonly ICategoriaDespesaRepository _categoriaRepository;

    public ListarCategoriasEndpoint(ICategoriaDespesaRepository categoriaRepository)
    {
        _categoriaRepository = categoriaRepository;
    }

    public override void Configure()
    {
        Get("usuarios/{idUsuario}/categorias");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ListarCategoriasRequest req, CancellationToken ct)
    {
        IReadOnlyList<CategoriaDespesa> categorias = await _categoriaRepository.ListarAsync(ct);

        ListarCategoriasResponse.Categoria[] categoriasResponse = categorias
            .OrderBy(c => c.Nome.Value, StringComparer.CurrentCultureIgnoreCase)
            .Select(Ler)
            .ToArray();

        await Send.OkAsync(
            new ListarCategoriasResponse(categoriasResponse),
            ct);
    }

    private static ListarCategoriasResponse.Categoria Ler(CategoriaDespesa categoria)
    {
        return new(
            categoria.Id,
            categoria.Nome,
            categoria.Aliases.Select(a => a.Alias.TextoOriginal).ToArray());
    }
}
