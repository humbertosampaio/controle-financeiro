using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Shared.ValueObjects;

public sealed class DiaVencimentoCartaoTests
{
    public static readonly TheoryData<int> DiasValidos = new(1, 14, 28);
    public static readonly TheoryData<int> DiasInvalidos = new(0, -1, 29, 31);

    [Theory]
    [MemberData(nameof(DiasValidos))]
    public void Criar_Quando_DiaEntre1e28_Deve_RetornarSucesso(int dia)
    {
        Result<DiaVencimentoCartao> result = DiaVencimentoCartao.Criar(dia);

        DiaVencimentoCartao value = result.Should().Succeed().Which;
        value.Value.Should().Be(dia);
    }

    [Theory]
    [MemberData(nameof(DiasInvalidos))]
    public void Criar_Quando_DiaMenorQue1OuMaiorQue28_Deve_RetornarFalha(int dia)
    {
        Result<DiaVencimentoCartao> result = DiaVencimentoCartao.Criar(dia);

        result.Should().FailWith($"O dia de vencimento do cartão deve estar entre 1 e 28. Valor fornecido: {dia}");
    }

    [Theory]
    [MemberData(nameof(DiasValidos))]
    public void OperadorExplicito_Quando_ValorValido_Deve_RetornarInstancia(int dia)
    {
        DiaVencimentoCartao result = (DiaVencimentoCartao)dia;

        result.Value.Should().Be(dia);
    }

    [Theory]
    [MemberData(nameof(DiasInvalidos))]
    public void OperadorExplicito_Quando_ValorInvalido_Deve_LancarExcecao(int dia)
    {
        Func<DiaVencimentoCartao> act = () => (DiaVencimentoCartao)dia;

        act.Should().ThrowExactly<ResultFailureException>();
    }

    [Theory, DomainAutoData]
    public void Equals_Quando_MesmoDia_Deve_RetornarTrue([Frozen] DiaVencimentoCartao diaVencimento1, DiaVencimentoCartao diaVencimento2)
    {
        diaVencimento1.Should().Be(diaVencimento2);
        (diaVencimento1 == diaVencimento2).Should().BeTrue();
        diaVencimento1.GetHashCode().Should().Be(diaVencimento2.GetHashCode());
    }

    [Fact]
    public void Equals_Quando_DiasDiferentes_Deve_RetornarFalse()
    {
        DiaVencimentoCartao diaVencimento1 = (DiaVencimentoCartao)1;
        DiaVencimentoCartao diaVencimento2 = (DiaVencimentoCartao)28;

        diaVencimento1.Should().NotBe(diaVencimento2);
        (diaVencimento1 == diaVencimento2).Should().BeFalse();
        diaVencimento1.GetHashCode().Should().NotBe(diaVencimento2.GetHashCode());
    }
}
