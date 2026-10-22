using ControleFinanceiro.Domain.Contas;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Contas;

public sealed class SufixoContaTests
{
    public static readonly TheoryData<string> SufixosValidos = new("0000", "1234", "9999", "123X");
    public static readonly TheoryData<string?> SufixosInvalidos =
        new(null, "", "   ", "123", "12345", "12a4", "X234", "12X4", "١٢٣٤");

    [Theory]
    [MemberData(nameof(SufixosValidos))]
    public void Criar_Quando_QuatroCaracteresValidos_Deve_RetornarSucesso(string sufixo)
    {
        Result<SufixoConta> result = SufixoConta.Criar(sufixo);

        result.Should().Succeed().Which.Value.Should().Be(sufixo);
    }

    [Theory]
    [InlineData("123-4", "1234")]
    [InlineData("1.234", "1234")]
    [InlineData(" 12 3-x ", "123X")]
    public void Criar_Quando_ComSeparadores_Deve_RetornarSufixoSemSeparadores(string sufixo, string esperado)
    {
        Result<SufixoConta> result = SufixoConta.Criar(sufixo);

        result.Should().Succeed().Which.Value.Should().Be(esperado);
    }

    [Fact]
    public void Criar_Quando_DigitoVerificadorXMinusculo_Deve_RetornarXMaiusculo()
    {
        Result<SufixoConta> result = SufixoConta.Criar("123x");

        result.Should().Succeed().Which.Value.Should().Be("123X");
    }

    [Theory]
    [MemberData(nameof(SufixosInvalidos))]
    public void Criar_Quando_SufixoMalformado_Deve_RetornarFalha(string? sufixo)
    {
        Result<SufixoConta> result = SufixoConta.Criar(sufixo);

        result.Should().FailWith("O sufixo da conta deve ter 4 caracteres: dígitos, com X permitido só no dígito verificador");
    }

    [Theory]
    [MemberData(nameof(SufixosValidos))]
    public void OperadorExplicito_Quando_ValorValido_Deve_RetornarInstancia(string sufixo)
    {
        SufixoConta result = (SufixoConta)sufixo;

        result.Value.Should().Be(sufixo);
    }

    [Theory]
    [MemberData(nameof(SufixosInvalidos))]
    public void OperadorExplicito_Quando_ValorInvalido_Deve_LancarExcecao(string? sufixo)
    {
        Func<SufixoConta> act = () => (SufixoConta)sufixo;

        act.Should().ThrowExactly<ResultFailureException>();
    }
}
