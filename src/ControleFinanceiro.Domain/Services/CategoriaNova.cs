using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Services;

/// <summary>Uma categoria nova, com o <see cref="Alias"/> quando ele vem de um arquivo.</summary>
public sealed class CategoriaNova : IItemLoteCategorias
{
    public TextoObrigatorio Nome { get; }

    /// <summary>Nulo no cadastro manual, sem arquivo.</summary>
    public Alias? Alias { get; }

    public CategoriaNova(TextoObrigatorio nome, Alias? alias)
    {
        Nome = nome;
        Alias = alias;
    }
}
