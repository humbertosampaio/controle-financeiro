using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using CSharpFunctionalExtensions;
using FastEndpoints;
using Lote = (System.Collections.Generic.IReadOnlyList<ControleFinanceiro.Domain.Categorias.CategoriaDespesa> CategoriasDoUsuario, System.Collections.Generic.IReadOnlyList<ControleFinanceiro.Domain.Services.IItemLoteCategorias> Itens);
using Resolvido = (System.Collections.Generic.IReadOnlyList<ControleFinanceiro.Domain.Categorias.CategoriaDespesa> Novas, System.Collections.Generic.IReadOnlyList<ControleFinanceiro.Domain.Categorias.CategoriaDespesa> Destinos);

namespace ControleFinanceiro.WebApi.Endpoints.Categorias.CadastrarLote;

/// <summary>
/// Cadastra categorias e associa a elas as categorias do banco, num lote aplicado inteiro ou não aplicado. Serve à
/// resolução da prévia da importação, em que o texto vira alias de uma categoria existente ou de uma nova, e ao
/// cadastro manual, em que a categoria nova vem sem alias.
/// </summary>
public sealed class CadastrarLoteCategoriasEndpoint : Endpoint<CadastrarLoteCategoriasRequest, CadastrarLoteCategoriasResponse>
{
    private readonly IUsuarioAtual _usuarioAtual;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ICategoriaDespesaRepository _categoriaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CadastrarLoteCategoriasEndpoint(
        IUsuarioAtual usuarioAtual,
        IUsuarioRepository usuarioRepository,
        ICategoriaDespesaRepository categoriaRepository,
        IUnitOfWork unitOfWork)
    {
        _usuarioAtual = usuarioAtual;
        _usuarioRepository = usuarioRepository;
        _categoriaRepository = categoriaRepository;
        _unitOfWork = unitOfWork;
    }

    public override void Configure()
    {
        Post("usuarios/{idUsuario}/categorias/lote");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CadastrarLoteCategoriasRequest req, CancellationToken ct)
    {
        await _usuarioRepository.ObterPorIdAsync(_usuarioAtual.Id, ct)
            .ToResult(Falha.NaoEncontrado)
            .Bind(_ => LerLoteAsync(req.Itens, ct))
            .Bind(Resolver)
            .Tap(resolvido => GravarAsync(resolvido.Novas, ct))
            .Match(
                resolvido => Send.OkAsync(new CadastrarLoteCategoriasResponse([.. resolvido.Destinos.Select(c => (Guid)c.Id)]), ct),
                falha => EnviarFalhaAsync(falha, ct));
    }

    /// <summary>A categoria existente que um item cita e o usuário não tem interrompe o lote.</summary>
    private async Task<Result<Lote, Falha>> LerLoteAsync(IReadOnlyList<ItemCategoriaDto> itens, CancellationToken ct)
    {
        IReadOnlyList<CategoriaDespesa> categoriasDoUsuario = await _categoriaRepository.ListarAsync(ct);
        List<IItemLoteCategorias> lote = [];

        foreach (ItemCategoriaDto item in itens)
        {
            if (item.IdCategoria is not Guid guid)
            {
                lote.Add(new CategoriaNova((TextoObrigatorio)item.Nome, item.Alias is null ? null : (Alias)item.Alias));
                continue;
            }

            IdCategoriaDespesa idCategoria = IdCategoriaDespesa.From(guid);
            if (categoriasDoUsuario.FirstOrDefault(c => c.Id == idCategoria) is not CategoriaDespesa categoria)
                return Falha.NaoEncontrado;

            lote.Add(new AliasCategoriaExistente(categoria, (Alias)item.Alias));
        }

        return (categoriasDoUsuario, lote);
    }

    private Result<Resolvido, Falha> Resolver(Lote lote)
    {
        return CategoriaDespesaFactory
            .Resolver(_usuarioAtual.Id, lote.CategoriasDoUsuario, lote.Itens)
            .MapError(Falha.Conflito)
            .Map(novas => (novas, Destinos(lote.Itens, novas)));
    }

    /// <summary>A categoria de cada item. As novas saem da factory na ordem dos itens que as criaram.</summary>
    private static IReadOnlyList<CategoriaDespesa> Destinos(
        IReadOnlyList<IItemLoteCategorias> itens,
        IReadOnlyList<CategoriaDespesa> novas)
    {
        Queue<CategoriaDespesa> proximas = new(novas);
        return itens
            .Select(item => item is AliasCategoriaExistente existente ? existente.Categoria : proximas.Dequeue())
            .ToList();
    }

    private async Task GravarAsync(IReadOnlyList<CategoriaDespesa> novas, CancellationToken ct)
    {
        await _categoriaRepository.AdicionarTodasAsync(novas, ct);
        await _unitOfWork.CommitAsync(ct);
    }

    private async Task EnviarFalhaAsync(Falha falha, CancellationToken ct)
    {
        switch (falha)
        {
            case Falha.Ausente:
                await Send.NotFoundAsync(ct);
                break;

            case Falha.Conflitante(Erro erro) when erro is ErroCategoriaJaCadastrada or ErroAliasCategoriaJaAtribuido:
                AddError(r => r.Itens, erro.Descricao);
                await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
                break;

            default:
                throw new InvalidOperationException($"Falha sem status mapeado: {falha}");
        }
    }

    /// <summary>
    /// O que interrompe o cadastro, num tipo só para a cadeia: o usuário ou a categoria de um item que não existe, e o
    /// erro do domínio.
    /// </summary>
    private abstract record Falha
    {
        public static readonly Falha NaoEncontrado = new Ausente();

        public static Falha Conflito(Erro erro) => new Conflitante(erro);

        public sealed record Ausente : Falha;

        public sealed record Conflitante(Erro Erro) : Falha;
    }
}
