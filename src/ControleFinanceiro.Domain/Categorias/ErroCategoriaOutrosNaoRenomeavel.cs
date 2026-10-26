using ControleFinanceiro.Domain.Shared;

namespace ControleFinanceiro.Domain.Categorias;

/// <summary>
/// A categoria <see cref="CategoriaDespesa.NomeOutros"/> não pode ser renomeada, porque é achada pelo nome.
/// </summary>
public sealed class ErroCategoriaOutrosNaoRenomeavel : Erro
{
    public static readonly ErroCategoriaOutrosNaoRenomeavel Instancia = new();

    internal ErroCategoriaOutrosNaoRenomeavel()
        : base("Categorias.OutrosNaoRenomeavel", $"A categoria {CategoriaDespesa.NomeOutros} não pode ser renomeada.")
    {
    }
}
