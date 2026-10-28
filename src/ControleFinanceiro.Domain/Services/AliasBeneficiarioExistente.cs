using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Services;

/// <summary>O <see cref="Alias"/> passa a apontar para o <see cref="Beneficiario"/>, que o usuário já tem.</summary>
public sealed class AliasBeneficiarioExistente : IItemLoteBeneficiarios
{
    public Beneficiario Beneficiario { get; }
    public Alias Alias { get; }

    public AliasBeneficiarioExistente(Beneficiario beneficiario, Alias alias)
    {
        Beneficiario = beneficiario;
        Alias = alias;
    }
}
