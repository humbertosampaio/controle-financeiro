using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Services;

/// <summary>O <see cref="Alias"/> passa a apontar para a <see cref="Categoria"/>, que o usuário já tem.</summary>
public sealed class AliasCategoriaExistente : IItemLoteCategorias
{
    public CategoriaDespesa Categoria { get; }
    public Alias Alias { get; }

    public AliasCategoriaExistente(CategoriaDespesa categoria, Alias alias)
    {
        Categoria = categoria;
        Alias = alias;
    }
}
