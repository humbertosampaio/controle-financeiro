using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Shared.ValueObjects;

public sealed class ReaisTests
{
    [Theory, DomainAutoData]
    public void Construtor_Quando_ValorDecimal_Deve_ExporValor(decimal valor)
    {
        Reais sut = new(valor);

        sut.Value.Should().Be(valor);
    }

    [Theory]
    [InlineData(0, "R$ 0,00")]
    [InlineData(10, "R$ 10,00")]
    [InlineData(15.3, "R$ 15,30")]
    [InlineData(15.344, "R$ 15,34")]
    [InlineData(15.345, "R$ 15,35")]
    [InlineData(15.346, "R$ 15,35")]
    public void ToString_Deve_RetornarStringFormatadaComoValorMonetarioComDuasCasasDecimais(decimal valor, string resultadoEsperado)
    {
        Reais sut = new(valor);

        string resultado = sut.ToString();

        resultado.Should().Be(resultadoEsperado);
    }

    [Fact]
    public void ToString_Quando_ValorNegativo_Deve_RetornarStringFormatadaComSinal()
    {
        Reais sut = new(-10);

        string resultado = sut.ToString();

        resultado.Should().Be("-R$ 10,00");
    }

    [Theory, DomainAutoData]
    public void Equals_Quando_MesmoValor_Deve_RetornarTrue([Frozen] decimal valor)
    {
        Reais reais1 = new(valor);
        Reais reais2 = new(valor);

        reais1.Should().Be(reais2);
        (reais1 == reais2).Should().BeTrue();
        reais1.GetHashCode().Should().Be(reais2.GetHashCode());
    }

    [Theory, DomainAutoData]
    public void Equals_Quando_ValoresDiferentes_Deve_RetornarFalse(decimal valor1, decimal valor2)
    {
        Reais reais1 = new(valor1);
        Reais reais2 = new(valor2);

        reais1.Should().NotBe(reais2);
        (reais1 == reais2).Should().BeFalse();
        reais1.GetHashCode().Should().NotBe(reais2.GetHashCode());
    }

    [Theory, DomainAutoData]
    public void OperadorSoma_Quando_DoisValores_Deve_RetornarSoma(decimal valor1, decimal valor2)
    {
        Reais reais1 = new(valor1);
        Reais reais2 = new(valor2);

        Reais resultado = reais1 + reais2;

        resultado.Value.Should().Be(valor1 + valor2);
    }

    [Theory, DomainAutoData]
    public void OperadorSubtracao_Quando_DoisValores_Deve_RetornarDiferenca(decimal valor1, decimal valor2)
    {
        Reais reais1 = new(valor1);
        Reais reais2 = new(valor2);

        Reais resultado = reais1 - reais2;

        resultado.Value.Should().Be(valor1 - valor2);
    }

    [Theory, DomainAutoData]
    public void OperadorImplicitoDecimalParaReais_Deve_RetornarInstanciaEquivalente(decimal valor)
    {
        Reais resultado = valor;

        resultado.Value.Should().Be(valor);
    }

    [Theory, DomainAutoData]
    public void OperadorImplicitoReaisParaDecimal_Deve_RetornarValor(Reais reais)
    {
        decimal resultado = reais;

        resultado.Should().Be(reais.Value);
    }
}
