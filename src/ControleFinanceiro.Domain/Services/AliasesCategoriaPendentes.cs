using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// As categorias do banco de um arquivo que ainda não são alias de nenhuma categoria do usuário: o que ele precisa
/// resolver antes de importar o arquivo. Vale para todo lançamento, inclusive a entrada e o estorno, que abate o gasto
/// da categoria no balanço.
/// </summary>
public static class AliasesCategoriaPendentes
{
    /// <param name="categoriasDoUsuario">Todas as categorias do usuário, e só as dele.</param>
    /// <param name="lancamentos">Os lançamentos do arquivo, na ordem dele.</param>
    /// <returns>
    /// Um alias por forma normalizada, com o texto original da primeira vez que ela aparece, na ordem do arquivo. O
    /// lançamento sem categoria do banco fica de fora, assim como o texto que fica vazio depois de normalizado.
    /// </returns>
    /// <exception cref="ArgumentException">Quando a lista traz uma categoria de outro usuário.</exception>
    public static IReadOnlyList<Alias> Listar(
        IdUsuario idUsuario,
        IReadOnlyCollection<CategoriaDespesa> categoriasDoUsuario,
        IEnumerable<Lancamento> lancamentos)
    {
        if (categoriasDoUsuario.Any(c => c.IdUsuario != idUsuario))
            throw new ArgumentException("A lista traz uma categoria de outro usuário.", nameof(categoriasDoUsuario));

        return lancamentos
            .Select(l => Alias.Criar(l.CategoriaBanco))
            .Where(alias => alias.IsSuccess)
            .Select(alias => alias.Value)
            .Distinct()
            .Where(alias => categoriasDoUsuario.All(c => !c.PossuiAlias(alias)))
            .ToList();
    }
}
