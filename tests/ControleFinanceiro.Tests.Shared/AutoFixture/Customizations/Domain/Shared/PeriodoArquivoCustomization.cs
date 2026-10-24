using ControleFinanceiro.Domain.Importacoes;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Customizations.Domain.Shared;

/// <summary>
/// Período válido: começa na data do fixture e dura até 255 dias além dela.
/// </summary>
internal sealed class PeriodoArquivoCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((DateOnly inicio, byte diasAlemDoInicio) =>
            PeriodoArquivo.Criar(inicio, inicio.AddDays(diasAlemDoInicio)).Value);
    }
}
