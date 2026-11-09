using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// O beneficiário e a categoria de uma compra da fatura, pelos aliases que a <see cref="LoteTransacoesFactory"/> já
/// conferiu. Serve às factories que criam despesas da fatura.
/// </summary>
internal static class AliasesResolvidos
{
    /// <exception cref="ArgumentException">Quando nenhum beneficiário da lista tem o alias.</exception>
    public static IdBeneficiario IdBeneficiarioDo(IReadOnlyCollection<Beneficiario> beneficiariosDoUsuario, Alias alias)
    {
        Beneficiario beneficiario = beneficiariosDoUsuario.FirstOrDefault(b => b.PossuiAlias(alias))
            ?? throw new ArgumentException("O beneficiário de uma compra não está na lista.", nameof(beneficiariosDoUsuario));

        return beneficiario.Id;
    }

    /// <returns>Nulo quando o banco não dá categoria à compra.</returns>
    /// <exception cref="ArgumentException">Quando nenhuma categoria da lista tem o alias.</exception>
    public static IdCategoriaDespesa? IdCategoriaDo(IReadOnlyCollection<CategoriaDespesa> categoriasDoUsuario, Alias? alias)
    {
        if (alias is null)
            return null;

        CategoriaDespesa categoria = categoriasDoUsuario.FirstOrDefault(c => c.PossuiAlias(alias))
            ?? throw new ArgumentException("A categoria de uma compra não está na lista.", nameof(categoriasDoUsuario));

        return categoria.Id;
    }
}
