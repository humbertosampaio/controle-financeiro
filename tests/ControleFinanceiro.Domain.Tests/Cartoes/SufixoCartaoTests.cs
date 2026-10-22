using ControleFinanceiro.Domain.Cartoes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Cartoes;

public sealed class SufixoCartaoTests
{
    public static readonly TheoryData<string> SufixosValidos = new("0000", "1234", "9999");
    public static readonly TheoryData<string?> SufixosInvalidos = new(null, "", "   ", "123", "12345", "12a4", "12 4", "١٢٣٤");

    [Theory]
    [MemberData(nameof(SufixosValidos))]
    public void Criar_Quando_QuatroDigitos_Deve_RetornarSucesso(string sufixo)
    {
        Result<SufixoCartao> result = SufixoCartao.Criar(sufixo);

        result.Should().Succeed().Which.Value.Should().Be(sufixo);
    }

    [Fact]
    public void Criar_Quando_EspacosNasPontas_Deve_RetornarSufixoAparado()
    {
        Result<SufixoCartao> result = SufixoCartao.Criar(" 1234 ");

        result.Should().Succeed().Which.Value.Should().Be("1234");
    }

    [Theory]
    [MemberData(nameof(SufixosInvalidos))]
    public void Criar_Quando_NaoSaoQuatroDigitos_Deve_RetornarFalha(string? sufixo)
    {
        Result<SufixoCartao> result = SufixoCartao.Criar(sufixo);

        result.Should().FailWith("O sufixo do cartão deve ter exatamente 4 dígitos");
    }

    [Theory]
    [MemberData(nameof(SufixosValidos))]
    public void OperadorExplicito_Quando_ValorValido_Deve_RetornarInstancia(string sufixo)
    {
        SufixoCartao result = (SufixoCartao)sufixo;

        result.Value.Should().Be(sufixo);
    }

    [Theory]
    [MemberData(nameof(SufixosInvalidos))]
    public void OperadorExplicito_Quando_ValorInvalido_Deve_LancarExcecao(string? sufixo)
    {
        Func<SufixoCartao> act = () => (SufixoCartao)sufixo;

        act.Should().ThrowExactly<ResultFailureException>();
    }
}
