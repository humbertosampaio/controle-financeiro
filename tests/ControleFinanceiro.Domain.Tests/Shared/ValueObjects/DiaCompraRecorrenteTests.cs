using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Shared.ValueObjects;

public sealed class DiaCompraRecorrenteTests
{
    public static readonly TheoryData<int> DiasValidos = new(1, 14, 28);
    public static readonly TheoryData<int> DiasInvalidos = new(0, -1, 29, 31);

    [Theory]
    [MemberData(nameof(DiasValidos))]
    public void Criar_Quando_DiaEntre1e28_Deve_RetornarSucesso(int dia)
    {
        Result<DiaCompraRecorrente> result = DiaCompraRecorrente.Criar(dia);

        DiaCompraRecorrente value = result.Should().Succeed().Which;
        value.Value.Should().Be(dia);
    }

    [Theory]
    [MemberData(nameof(DiasInvalidos))]
    public void Criar_Quando_DiaMenorQue1OuMaiorQue28_Deve_RetornarFalha(int dia)
    {
        Result<DiaCompraRecorrente> result = DiaCompraRecorrente.Criar(dia);

        result.Should().FailWith($"O dia da compra de uma compra recorrente deve estar entre 1 e 28. Valor fornecido: {dia}");
    }

    [Theory]
    [MemberData(nameof(DiasValidos))]
    public void OperadorExplicito_Quando_ValorValido_Deve_RetornarInstancia(int dia)
    {
        DiaCompraRecorrente result = (DiaCompraRecorrente)dia;

        result.Value.Should().Be(dia);
    }

    [Theory]
    [MemberData(nameof(DiasInvalidos))]
    public void OperadorExplicito_Quando_ValorInvalido_Deve_LancarExcecao(int dia)
    {
        Func<DiaCompraRecorrente> act = () => (DiaCompraRecorrente)dia;

        act.Should().ThrowExactly<ResultFailureException>();
    }

    [Theory, DomainAutoData]
    public void Equals_Quando_MesmoDia_Deve_RetornarTrue([Frozen] DiaCompraRecorrente diaCompra1, DiaCompraRecorrente diaCompra2)
    {
        diaCompra1.Should().Be(diaCompra2);
        (diaCompra1 == diaCompra2).Should().BeTrue();
        diaCompra1.GetHashCode().Should().Be(diaCompra2.GetHashCode());
    }

    [Fact]
    public void Equals_Quando_DiasDiferentes_Deve_RetornarFalse()
    {
        DiaCompraRecorrente diaCompra1 = (DiaCompraRecorrente)1;
        DiaCompraRecorrente diaCompra2 = (DiaCompraRecorrente)28;

        diaCompra1.Should().NotBe(diaCompra2);
        (diaCompra1 == diaCompra2).Should().BeFalse();
        diaCompra1.GetHashCode().Should().NotBe(diaCompra2.GetHashCode());
    }
}
