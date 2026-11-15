using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Customizations.Domain.Shared;

/// <summary>
/// Período válido: começa na data do fixture e dura até 255 dias além dela.
/// </summary>
internal sealed class PeriodoCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((DateOnly inicio, byte diasAlemDoInicio) =>
            new Periodo(inicio, inicio.AddDays(diasAlemDoInicio)));
    }
}
