using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Shared.ValueObjects;

public sealed class ReaisPositivoTests
{
    [Theory]
    [InlineDomainAutoData]
    [InlineData(0)]
    public void Criar_Quando_ValorMaiorOuIgualAZero_Deve_RetornarSucesso(decimal valor)
    {
        Result<ReaisPositivo> result = ReaisPositivo.Criar(valor);

        result.Should().Succeed()
            .And.Subject.Value.AsDecimal().Should().Be(valor);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_ValorMenorQueZero_Deve_RetornarFalha(decimal valor)
    {
        valor = -valor;

        Result<ReaisPositivo> result = ReaisPositivo.Criar(valor);

        result.Should().FailWith($"O valor deve ser positivo. Encontrado: {valor}");
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_ParametroReais_Deve_RetornarInstanciaEquivalenteADecimal(Reais reais)
    {
        decimal valorDecimal = reais;

        Result<ReaisPositivo> resultReais = ReaisPositivo.Criar(reais);
        Result<ReaisPositivo> resultDecimal = ReaisPositivo.Criar(valorDecimal);

        resultReais.Should().Succeed();
        resultDecimal.Should().Succeed();
        resultReais.Value.Should().Be(resultDecimal.Value);
    }

    [Theory, DomainAutoData]
    public void OperadorExplicitoDecimalParaReaisPositivo_Quando_ValorValido_Deve_RetornarInstancia(decimal valor)
    {
        ReaisPositivo result = (ReaisPositivo)valor;

        result.AsDecimal().Should().Be(valor);
    }

    [Theory, DomainAutoData]
    public void OperadorExplicitoDecimalParaReaisPositivo_Quando_ValorInvalido_Deve_LancarExcecao(decimal valor)
    {
        Func<ReaisPositivo> result = () => (ReaisPositivo)(-valor);

        result.Should().ThrowExactly<ResultFailureException>();
    }
}

internal static class ReaisPositivoExtensions
{
    extension(ReaisPositivo reaisPositivo)
    {
        public decimal AsDecimal() => reaisPositivo;
    }
}