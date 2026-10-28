using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Services;

/// <summary>Um beneficiário novo, com o <see cref="Alias"/> quando ele vem de um arquivo.</summary>
public sealed class BeneficiarioNovo : IItemLoteBeneficiarios
{
    public TextoObrigatorio Nome { get; }

    /// <summary>Nulo no cadastro manual, sem arquivo.</summary>
    public Alias? Alias { get; }

    public BeneficiarioNovo(TextoObrigatorio nome, Alias? alias)
    {
        Nome = nome;
        Alias = alias;
    }
}
