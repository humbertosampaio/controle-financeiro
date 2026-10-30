using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Cartoes;

public sealed class DiaFechamentoCartaoTests
{
    public static readonly TheoryData<int> DiasValidos = new(1, 14, 28);
    public static readonly TheoryData<int> DiasInvalidos = new(0, -1, 29, 31);

    [Theory]
    [MemberData(nameof(DiasValidos))]
    public void Criar_Quando_DiaEntre1e28_Deve_RetornarSucesso(int dia)
    {
        Result<DiaFechamentoCartao> result = DiaFechamentoCartao.Criar(dia);

        DiaFechamentoCartao value = result.Should().Succeed().Which;
        value.Value.Should().Be(dia);
    }

    [Theory]
    [MemberData(nameof(DiasInvalidos))]
    public void Criar_Quando_DiaMenorQue1OuMaiorQue28_Deve_RetornarFalha(int dia)
    {
        Result<DiaFechamentoCartao> result = DiaFechamentoCartao.Criar(dia);

        result.Should().FailWith($"O dia de fechamento do cartão deve estar entre 1 e 28. Valor fornecido: {dia}");
    }

    [Theory]
    [MemberData(nameof(DiasValidos))]
    public void OperadorExplicito_Quando_ValorValido_Deve_RetornarInstancia(int dia)
    {
        DiaFechamentoCartao result = (DiaFechamentoCartao)dia;

        result.Value.Should().Be(dia);
    }

    [Theory]
    [MemberData(nameof(DiasInvalidos))]
    public void OperadorExplicito_Quando_ValorInvalido_Deve_LancarExcecao(int dia)
    {
        Func<DiaFechamentoCartao> act = () => (DiaFechamentoCartao)dia;

        act.Should().ThrowExactly<ResultFailureException>();
    }

    [Theory, DomainAutoData]
    public void Equals_Quando_MesmoDia_Deve_RetornarTrue([Frozen] DiaFechamentoCartao diaFechamento1, DiaFechamentoCartao diaFechamento2)
    {
        diaFechamento1.Should().Be(diaFechamento2);
        (diaFechamento1 == diaFechamento2).Should().BeTrue();
        diaFechamento1.GetHashCode().Should().Be(diaFechamento2.GetHashCode());
    }

    [Fact]
    public void Equals_Quando_DiasDiferentes_Deve_RetornarFalse()
    {
        DiaFechamentoCartao diaFechamento1 = (DiaFechamentoCartao)1;
        DiaFechamentoCartao diaFechamento2 = (DiaFechamentoCartao)28;

        diaFechamento1.Should().NotBe(diaFechamento2);
        (diaFechamento1 == diaFechamento2).Should().BeFalse();
        diaFechamento1.GetHashCode().Should().NotBe(diaFechamento2.GetHashCode());
    }
}
