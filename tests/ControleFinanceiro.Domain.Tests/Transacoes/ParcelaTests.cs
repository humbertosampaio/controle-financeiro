using ControleFinanceiro.Domain.Transacoes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Transacoes;

public sealed class ParcelaTests
{
    public static readonly TheoryData<int, int> ParesValidos = new()
    {
        { 1, 2 },
        { 2, 2 },
        { 1, 10 },
        { 10, 10 },
    };

    public static readonly TheoryData<int, int> ParesInvalidos = new()
    {
        { 0, 10 },
        { -1, 10 },
        { 11, 10 },
        { 1, 1 },
        { 1, 0 },
        { 0, 0 },
        { 1, -1 },
    };

    [Theory]
    [MemberData(nameof(ParesValidos))]
    public void Criar_Quando_TotalAPartirDe2ENumeroEntre1EOTotal_Deve_RetornarSucesso(int numero, int total)
    {
        Result<Parcela> result = Parcela.Criar(numero, total);

        Parcela parcela = result.Should().Succeed().Subject;
        parcela.Numero.Should().Be(numero);
        parcela.Total.Should().Be(total);
    }

    [Theory]
    [MemberData(nameof(ParesInvalidos))]
    public void Criar_Quando_TotalMenorQue2OuNumeroForaDe1AoTotal_Deve_RetornarFalha(int numero, int total)
    {
        Result<Parcela> result = Parcela.Criar(numero, total);

        result.Should().FailWith(
            $"A parcela deve ter o total a partir de 2 e o número entre 1 e o total. Valores fornecidos: {numero} de {total}");
    }

    [Theory]
    [MemberData(nameof(ParesValidos))]
    public void OperadorExplicito_Quando_ParValido_Deve_RetornarInstancia(int numero, int total)
    {
        Parcela result = (Parcela)(numero, total);

        result.Numero.Should().Be(numero);
        result.Total.Should().Be(total);
    }

    [Theory]
    [MemberData(nameof(ParesInvalidos))]
    public void OperadorExplicito_Quando_ParInvalido_Deve_LancarExcecao(int numero, int total)
    {
        Func<Parcela> act = () => (Parcela)(numero, total);

        act.Should().ThrowExactly<ResultFailureException>();
    }

    [Fact]
    public void Equals_Quando_MesmoNumeroEMesmoTotal_Deve_SerIgual()
    {
        ((Parcela)(3, 10)).Should().Be((Parcela)(3, 10));
    }

    [Fact]
    public void Equals_Quando_TotaisDiferentes_Deve_SerDiferente()
    {
        ((Parcela)(3, 10)).Should().NotBe((Parcela)(3, 12));
    }
}
