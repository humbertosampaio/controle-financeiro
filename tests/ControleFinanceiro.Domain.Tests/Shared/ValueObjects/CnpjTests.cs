using ControleFinanceiro.Domain.Shared.ValueObjects;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Shared.ValueObjects;

/// <summary>
/// Os números são os exemplos públicos: o <c>12ABC34501DE35</c> é o que a Receita publicou para o CNPJ alfanumérico, e o
/// <c>11222333000181</c> é o exemplo clássico do numérico. Os inválidos são variações deles; o resto vem do
/// <c>CnpjGenerator</c>.
/// </summary>
public sealed class CnpjTests
{
    public static readonly TheoryData<string> CnpjsValidos = new("12ABC34501DE35", "11222333000181");

    public static readonly TheoryData<string?> CnpjsInvalidos = new(
        null,
        "",
        "1222333000181",
        "112223330001811",
        "11222333000191",
        "11222333000182",
        "00000000000000",
        "AAAAAAAAAAAA00",
        "12ABC34501DEA5",
        "12ABÇ34501DE35",
        "12ABC34501-E35");

    [Theory]
    [MemberData(nameof(CnpjsValidos))]
    public void Criar_Quando_VerificadoresConferem_Deve_RetornarSucesso(string numero)
    {
        Result<Cnpj> result = Cnpj.Criar(numero);

        result.Should().Succeed().Which.Value.Should().Be(numero);
    }

    [Theory]
    [InlineData("11.222.333/0001-81", "11222333000181")]
    [InlineData("12.ABC.345/01DE-35", "12ABC34501DE35")]
    [InlineData("12.abc.345/01de-35", "12ABC34501DE35")]
    public void Criar_Quando_ComMascaraOuMinusculas_Deve_GuardarSemMascaraEmMaiusculas(string numero, string esperado)
    {
        Result<Cnpj> result = Cnpj.Criar(numero);

        result.Should().Succeed().Which.Value.Should().Be(esperado);
    }

    [Theory]
    [MemberData(nameof(CnpjsInvalidos))]
    public void Criar_Quando_NumeroInvalido_Deve_RetornarFalhaSemONumero(string? numero)
    {
        Result<Cnpj> result = Cnpj.Criar(numero);

        result.Should().FailWith("O documento não é um CNPJ válido");
    }

    [Theory]
    [MemberData(nameof(CnpjsValidos))]
    public void OperadorExplicito_Quando_NumeroValido_Deve_RetornarInstancia(string numero)
    {
        Cnpj result = (Cnpj)numero;

        result.Value.Should().Be(numero);
    }

    [Theory]
    [MemberData(nameof(CnpjsInvalidos))]
    public void OperadorExplicito_Quando_NumeroInvalido_Deve_LancarExcecao(string? numero)
    {
        Func<Cnpj> act = () => (Cnpj)numero;

        act.Should().ThrowExactly<ResultFailureException>();
    }
}
