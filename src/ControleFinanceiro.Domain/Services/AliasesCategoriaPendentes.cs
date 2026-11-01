using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// As categorias do banco de um arquivo que ainda não são alias de nenhuma categoria do usuário: o que ele precisa
/// resolver antes de importar o arquivo. Vale para todo lançamento, inclusive a entrada e o estorno, que abate o gasto
/// da categoria no balanço. A prévia as lista, e a importação recusa o arquivo enquanto houver alguma.
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
            .Select(l => l.AliasCategoriaBanco)
            .OfType<Alias>()
            .Distinct()
            .Where(alias => categoriasDoUsuario.All(c => !c.PossuiAlias(alias)))
            .ToList();
    }

    /// <summary>Falha quando o <see cref="Listar"/> traz algum texto: o arquivo inteiro fica sem importar.</summary>
    /// <param name="categoriasDoUsuario">Todas as categorias do usuário, e só as dele.</param>
    /// <exception cref="ArgumentException">Quando a lista traz uma categoria de outro usuário.</exception>
    public static UnitResult<Erro> Conferir(
        IdUsuario idUsuario,
        IReadOnlyCollection<CategoriaDespesa> categoriasDoUsuario,
        IEnumerable<Lancamento> lancamentos)
    {
        return Listar(idUsuario, categoriasDoUsuario, lancamentos).Count == 0
            ? UnitResult.Success<Erro>()
            : ErroCategoriasBancoNaoResolvidas.Instancia;
    }
}
