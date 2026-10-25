using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Shared.ValueObjects;

/// <summary>Os números são os exemplos de teste conhecidos do CPF; o CNPJ vem do <c>CnpjGenerator</c>.</summary>
public sealed class CpfTests
{
    public static readonly TheoryData<string> CpfsValidos = new("12345678909", "98765432100", "00000000191");

    public static readonly TheoryData<string?> CpfsInvalidos = new(
        null,
        "",
        "1234567890",
        "123456789090",
        "12345678919",
        "12345678908",
        "11111111111",
        "1234567890A",
        "١٢٣٤٥٦٧٨٩٠٩");

    [Theory]
    [MemberData(nameof(CpfsValidos))]
    public void Criar_Quando_VerificadoresConferem_Deve_RetornarSucesso(string numero)
    {
        Result<Cpf> result = Cpf.Criar(numero);

        result.Should().Succeed().Which.Value.Should().Be(numero);
    }

    [Theory]
    [InlineData("123.456.789-09")]
    [InlineData(" 123 456 789 09 ")]
    public void Criar_Quando_ComMascara_Deve_GuardarSoOsDigitos(string numero)
    {
        Result<Cpf> result = Cpf.Criar(numero);

        result.Should().Succeed().Which.Value.Should().Be("12345678909");
    }

    [Theory]
    [MemberData(nameof(CpfsInvalidos))]
    public void Criar_Quando_NumeroInvalido_Deve_RetornarFalhaSemONumero(string? numero)
    {
        Result<Cpf> result = Cpf.Criar(numero);

        result.Should().FailWith("O documento não é um CPF válido");
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_CnpjValido_Deve_RetornarFalha(Cnpj cnpj)
    {
        Result<Cpf> result = Cpf.Criar(cnpj.Value);

        result.Should().Fail();
    }

    [Theory]
    [MemberData(nameof(CpfsValidos))]
    public void OperadorExplicito_Quando_NumeroValido_Deve_RetornarInstancia(string numero)
    {
        Cpf result = (Cpf)numero;

        result.Value.Should().Be(numero);
    }

    [Theory]
    [MemberData(nameof(CpfsInvalidos))]
    public void OperadorExplicito_Quando_NumeroInvalido_Deve_LancarExcecao(string? numero)
    {
        Func<Cpf> act = () => (Cpf)numero;

        act.Should().ThrowExactly<ResultFailureException>();
    }
}
