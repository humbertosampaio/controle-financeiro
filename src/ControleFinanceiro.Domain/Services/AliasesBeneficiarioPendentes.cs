using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Os textos de beneficiário das saídas de um arquivo que ainda não são alias de nenhum beneficiário do usuário: o que
/// ele precisa resolver antes de importar o arquivo.
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
        IEnumerable<Lancamento> lancamentos,
        TipoOrigem tipoOrigem)
    {
        if (beneficiariosDoUsuario.Any(b => b.IdUsuario != idUsuario))
            throw new ArgumentException("A lista traz um beneficiário de outro usuário.", nameof(beneficiariosDoUsuario));

        return lancamentos
            .Select(l => Alias.Criar(l.TextoBeneficiario(tipoOrigem)))
            .Where(alias => alias.IsSuccess)
            .Select(alias => alias.Value)
            .Distinct()
            .Where(alias => beneficiariosDoUsuario.All(b => !b.PossuiAlias(alias)))
            .ToList();
    }
}
