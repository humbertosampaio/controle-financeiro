using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

/// <summary>
/// Monta o <see cref="VencimentoFatura"/> no dia de vencimento de um cartão num mês, como o ciclo do cartão o projeta.
/// Com <c>[Frozen] MesReferencia</c>, todo vencimento cai nesse mês, cada um num dia.
/// </summary>
public sealed class VencimentoCicloCartaoAutoDataAttribute : DomainAutoDataAttribute
{
    public VencimentoCicloCartaoAutoDataAttribute()
        : base(fixture => fixture.Register((MesReferencia mes, DiaVencimentoCartao dia) => VencimentoFatura.Projetado(mes, dia)))
    {
    }
}
