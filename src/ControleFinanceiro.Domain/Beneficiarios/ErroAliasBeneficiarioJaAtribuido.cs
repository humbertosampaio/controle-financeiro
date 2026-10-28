using ControleFinanceiro.Domain.Shared;

namespace ControleFinanceiro.Domain.Beneficiarios;

/// <summary>O alias já aponta para outro beneficiário do usuário, o <see cref="IdBeneficiario"/>.</summary>
/// <remarks>
/// No lote da <see cref="Services.BeneficiarioFactory.Resolver"/>, o beneficiário pode ser um novo, criado por um item
/// anterior do mesmo lote: como o lote falha inteiro, ele não é gravado, e o id não existe no banco.
/// </remarks>
public sealed class ErroAliasBeneficiarioJaAtribuido : Erro
{
    public IdBeneficiario IdBeneficiario { get; }

    internal ErroAliasBeneficiarioJaAtribuido(IdBeneficiario idBeneficiario)
        : base("Beneficiarios.AliasJaAtribuido", "O alias já é de outro beneficiário do usuário.")
    {
        IdBeneficiario = idBeneficiario;
    }
}
