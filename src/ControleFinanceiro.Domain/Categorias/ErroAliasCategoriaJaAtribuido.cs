using ControleFinanceiro.Domain.Shared;

namespace ControleFinanceiro.Domain.Categorias;

/// <summary>O alias já aponta para outra categoria do usuário, a <see cref="IdCategoria"/>.</summary>
/// <remarks>
/// No lote da <see cref="Services.CategoriaDespesaFactory.Resolver"/>, a categoria pode ser uma nova, criada por um item
/// anterior do mesmo lote: como o lote falha inteiro, ela não é gravada, e o id não existe no banco.
/// </remarks>
public sealed class ErroAliasCategoriaJaAtribuido : Erro
{
    public IdCategoriaDespesa IdCategoria { get; }

    internal ErroAliasCategoriaJaAtribuido(IdCategoriaDespesa idCategoria)
        : base("Categorias.AliasJaAtribuido", "O alias já é de outra categoria do usuário.")
    {
        IdCategoria = idCategoria;
    }
}
