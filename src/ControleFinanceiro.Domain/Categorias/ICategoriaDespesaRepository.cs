namespace ControleFinanceiro.Domain.Categorias;

/// <summary>Enxerga só as categorias do usuário atual.</summary>
public interface ICategoriaDespesaRepository
{
    Task<IReadOnlyList<CategoriaDespesa>> ListarAsync(CancellationToken cancellationToken);

    /// <summary>A categoria de outro usuário não é encontrada, como a que não existe.</summary>
    Task<Maybe<CategoriaDespesa>> ObterAsync(IdCategoriaDespesa idCategoria, CancellationToken cancellationToken);

    Task AdicionarAsync(CategoriaDespesa categoria, CancellationToken cancellationToken);
}
