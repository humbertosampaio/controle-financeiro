using ControleFinanceiro.Domain.Shared;

namespace ControleFinanceiro.Domain.Categorias;

/// <summary>
/// O usuário já tem a categoria <see cref="IdCategoria"/> com o mesmo nome, sem contar caixa, acentos e espaços
/// repetidos.
/// </summary>
/// <remarks>
/// No lote da <see cref="Services.CategoriaDespesaFactory.Resolver"/>, a categoria pode ser uma nova, criada por um item
/// anterior do mesmo lote: como o lote falha inteiro, ela não é gravada, e o id não existe no banco.
/// </remarks>
public sealed class ErroCategoriaJaCadastrada : Erro
{
    public IdCategoriaDespesa IdCategoria { get; }

    internal ErroCategoriaJaCadastrada(IdCategoriaDespesa idCategoria)
        : base("Categorias.JaCadastrada", "O usuário já tem uma categoria com esse nome.")
    {
        IdCategoria = idCategoria;
    }
}
