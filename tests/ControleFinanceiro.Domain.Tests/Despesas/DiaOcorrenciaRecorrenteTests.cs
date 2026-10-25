using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Despesas;

public sealed class DiaOcorrenciaRecorrenteTests
{
    public static readonly TheoryData<int> DiasValidos = new(1, 14, 28);
    public static readonly TheoryData<int> DiasInvalidos = new(0, -1, 29, 31);

    [Theory]
    [MemberData(nameof(DiasValidos))]
    public void Criar_Quando_DiaEntre1e28_Deve_RetornarSucesso(int dia)
    {
        Result<DiaOcorrenciaRecorrente> result = DiaOcorrenciaRecorrente.Criar(dia);

        DiaOcorrenciaRecorrente value = result.Should().Succeed().Which;
        value.Value.Should().Be(dia);
    }

    [Theory]
    [MemberData(nameof(DiasInvalidos))]
    public void Criar_Quando_DiaMenorQue1OuMaiorQue28_Deve_RetornarFalha(int dia)
    {
        Result<DiaOcorrenciaRecorrente> result = DiaOcorrenciaRecorrente.Criar(dia);

        result.Should().FailWith($"O dia da ocorrência de uma despesa recorrente deve estar entre 1 e 28. Valor fornecido: {dia}");
    }

    [Theory]
    [MemberData(nameof(DiasValidos))]
    public void OperadorExplicito_Quando_ValorValido_Deve_RetornarInstancia(int dia)
    {
        DiaOcorrenciaRecorrente result = (DiaOcorrenciaRecorrente)dia;

        result.Value.Should().Be(dia);
    }

    [Theory]
    [MemberData(nameof(DiasInvalidos))]
    public void OperadorExplicito_Quando_ValorInvalido_Deve_LancarExcecao(int dia)
    {
        Func<DiaOcorrenciaRecorrente> act = () => (DiaOcorrenciaRecorrente)dia;

        act.Should().ThrowExactly<ResultFailureException>();
    }

    [Theory, DomainAutoData]
    public void Equals_Quando_MesmoDia_Deve_RetornarTrue([Frozen] DiaOcorrenciaRecorrente diaOcorrencia1, DiaOcorrenciaRecorrente diaOcorrencia2)
    {
        diaOcorrencia1.Should().Be(diaOcorrencia2);
        (diaOcorrencia1 == diaOcorrencia2).Should().BeTrue();
        diaOcorrencia1.GetHashCode().Should().Be(diaOcorrencia2.GetHashCode());
    }

    [Fact]
    public void Equals_Quando_DiasDiferentes_Deve_RetornarFalse()
    {
        DiaOcorrenciaRecorrente diaOcorrencia1 = (DiaOcorrenciaRecorrente)1;
        DiaOcorrenciaRecorrente diaOcorrencia2 = (DiaOcorrenciaRecorrente)28;

        diaOcorrencia1.Should().NotBe(diaOcorrencia2);
        (diaOcorrencia1 == diaOcorrencia2).Should().BeFalse();
        diaOcorrencia1.GetHashCode().Should().NotBe(diaOcorrencia2.GetHashCode());
    }
}
