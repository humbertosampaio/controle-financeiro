using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Cria o beneficiário do usuário e atribui aliases a ele, garantindo que cada alias aponte para um beneficiário só
/// em cada usuário. O índice único da tabela é a rede para duas requisições ao mesmo tempo.
/// </summary>
public static class BeneficiarioFactory
{
    /// <param name="beneficiariosDoUsuario">Todos os beneficiários do usuário, e só os dele.</param>
    /// <param name="alias">Nulo no cadastro manual, sem arquivo.</param>
    /// <exception cref="ArgumentException">Quando a lista traz um beneficiário de outro usuário.</exception>
    public static Result<Beneficiario, Erro> Criar(
        IdUsuario idUsuario,
        IReadOnlyCollection<Beneficiario> beneficiariosDoUsuario,
        TextoObrigatorio nome,
        Alias? alias)
    {
        GarantirDoUsuario(idUsuario, beneficiariosDoUsuario);

        if (alias is not null && beneficiariosDoUsuario.FirstOrDefault(b => b.PossuiAlias(alias)) is Beneficiario dono)
            return new ErroAliasBeneficiarioJaAtribuido(dono.Id);

        return Beneficiario.Criar(idUsuario, nome, alias);
    }

    /// <summary>O alias que o <paramref name="beneficiario"/> já tem não muda nada.</summary>
    /// <param name="beneficiariosDoUsuario">Todos os beneficiários do dono do <paramref name="beneficiario"/>, e só os dele.</param>
    /// <exception cref="ArgumentException">Quando a lista traz um beneficiário de outro usuário.</exception>
    public static UnitResult<Erro> AtribuirAlias(
        IReadOnlyCollection<Beneficiario> beneficiariosDoUsuario,
        Beneficiario beneficiario,
        Alias alias)
    {
        GarantirDoUsuario(beneficiario.IdUsuario, beneficiariosDoUsuario);

        if (beneficiariosDoUsuario.FirstOrDefault(b => b != beneficiario && b.PossuiAlias(alias)) is Beneficiario dono)
            return new ErroAliasBeneficiarioJaAtribuido(dono.Id);

        beneficiario.AdicionarAlias(alias);
        return UnitResult.Success<Erro>();
    }

    private static void GarantirDoUsuario(IdUsuario idUsuario, IReadOnlyCollection<Beneficiario> beneficiariosDoUsuario)
    {
        if (beneficiariosDoUsuario.Any(b => b.IdUsuario != idUsuario))
            throw new ArgumentException("A lista traz um beneficiário de outro usuário.", nameof(beneficiariosDoUsuario));
    }
}
