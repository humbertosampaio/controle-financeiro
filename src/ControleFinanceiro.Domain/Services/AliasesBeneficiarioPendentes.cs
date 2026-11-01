using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Os textos de beneficiário das saídas de um arquivo que ainda não são alias de nenhum beneficiário do usuário: o que
/// ele precisa resolver antes de importar o arquivo. A prévia os lista, e a importação recusa o arquivo enquanto houver
/// algum.
/// </summary>
public static class AliasesBeneficiarioPendentes
{
    /// <param name="beneficiariosDoUsuario">Todos os beneficiários do usuário, e só os dele.</param>
    /// <param name="lancamentos">Os lançamentos do arquivo, na ordem dele.</param>
    /// <returns>
    /// Um alias por forma normalizada, com o texto original da primeira vez que ela aparece, na ordem do arquivo. O
    /// lançamento sem texto (a entrada, o boleto) fica de fora, assim como o texto que fica vazio depois de normalizado.
    /// </returns>
    /// <exception cref="ArgumentException">Quando a lista traz um beneficiário de outro usuário.</exception>
    public static IReadOnlyList<Alias> Listar(
        IdUsuario idUsuario,
        IReadOnlyCollection<Beneficiario> beneficiariosDoUsuario,
        IEnumerable<Lancamento> lancamentos)
    {
        if (beneficiariosDoUsuario.Any(b => b.IdUsuario != idUsuario))
            throw new ArgumentException("A lista traz um beneficiário de outro usuário.", nameof(beneficiariosDoUsuario));

        return lancamentos
            .Select(l => Alias.Criar(l.TextoBeneficiario))
            .Where(alias => alias.IsSuccess)
            .Select(alias => alias.Value)
            .Distinct()
            .Where(alias => beneficiariosDoUsuario.All(b => !b.PossuiAlias(alias)))
            .ToList();
    }

    /// <summary>Falha quando o <see cref="Listar"/> traz algum texto: o arquivo inteiro fica sem importar.</summary>
    /// <param name="beneficiariosDoUsuario">Todos os beneficiários do usuário, e só os dele.</param>
    /// <exception cref="ArgumentException">Quando a lista traz um beneficiário de outro usuário.</exception>
    public static UnitResult<Erro> Conferir(
        IdUsuario idUsuario,
        IReadOnlyCollection<Beneficiario> beneficiariosDoUsuario,
        IEnumerable<Lancamento> lancamentos)
    {
        return Listar(idUsuario, beneficiariosDoUsuario, lancamentos).Count == 0
            ? UnitResult.Success<Erro>()
            : ErroBeneficiariosNaoResolvidos.Instancia;
    }
}
