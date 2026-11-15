using ControleFinanceiro.Domain.Shared.Extensions;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Tests.Shared.Extensions;

public sealed class ReaisExtensionsTests
{
    [Fact]
    public void Sum_Quando_SequenciaVazia_Deve_RetornarZero()
    {
        ReaisPositivo[] valores = [];

        ReaisPositivo result = valores.Sum(valor => valor);

        result.Should().Be(ReaisPositivo.Zero);
    }

    // Os valores são o assunto: a soma é calculada à mão.
    [Fact]
    public void Sum_Deve_SomarOsValoresSelecionados()
    {
        ReaisPositivo[] valores = [(ReaisPositivo)10.25m, (ReaisPositivo)0.75m, (ReaisPositivo)89m];

        ReaisPositivo result = valores.Sum(valor => valor);

        result.Should().Be((ReaisPositivo)100m);
    }

    // Os valores são o assunto: a soma com sinal é calculada à mão.
    [Fact]
    public void Sum_Quando_ValoresComSinal_Deve_SomarSemPerderOSinal()
    {
        Reais[] valores = [-100.50m, 20.25m, -0.75m];

        Reais result = valores.Sum(valor => valor);

        result.Should().Be((Reais)(-81m));
    }
}
