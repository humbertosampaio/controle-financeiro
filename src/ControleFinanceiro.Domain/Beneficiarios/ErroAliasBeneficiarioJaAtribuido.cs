using ControleFinanceiro.Domain.Shared;

namespace ControleFinanceiro.Domain.Beneficiarios;

/// <summary>O alias já aponta para outro beneficiário do usuário, o <see cref="IdBeneficiario"/>.</summary>
public sealed class ErroAliasBeneficiarioJaAtribuido : Erro
{
    public IdBeneficiario IdBeneficiario { get; }

    internal ErroAliasBeneficiarioJaAtribuido(IdBeneficiario idBeneficiario)
        : base("Beneficiarios.AliasJaAtribuido", "O alias já é de outro beneficiário do usuário.")
    {
        IdBeneficiario = idBeneficiario;
    }
}
