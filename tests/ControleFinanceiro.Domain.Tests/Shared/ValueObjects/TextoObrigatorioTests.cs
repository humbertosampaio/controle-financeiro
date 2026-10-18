using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Shared.ValueObjects;

public sealed class TextoObrigatorioTests
{
    [Theory, MemberData(nameof(ValoresVazios))]
    public void Criar_Quando_ValorVazio_Deve_RetornarFalha(string? valor)
    {
        const string nomeCampo = "Texto";

        Result<TextoObrigatorio> result = TextoObrigatorio.Criar(valor, nomeCampo);

        result.Should().FailWith($"{nomeCampo} é obrigatório");
    }

    [Theory, AutoData]
    public void Criar_Quando_ValorValido_Deve_RetornarSucesso(string valor)
    {
        Result<TextoObrigatorio> result = TextoObrigatorio.Criar(valor, "Texto");

        result.Should().Succeed().And.Subject.Value.Value.Should().Be(valor);
    }

    [Theory, MemberData(nameof(ValoresVazios))]
    public void OperadorExplicito_Quando_ValorVazio_Deve_RetornarFalha(string? valor)
    {
        Func<TextoObrigatorio> result = () => (TextoObrigatorio)valor;

        result.Should().ThrowExactly<ResultFailureException>();
    }

    [Theory, AutoData]
    public void OperadorExplicito_Quando_ValorValido_Deve_RetornarInstancia(string valor)
    {
        TextoObrigatorio result = (TextoObrigatorio)valor;

        result.Value.Should().Be(valor);
    }

    public static readonly TheoryData<string?> ValoresVazios = new(string.Empty, null, "   ", "\t");
}
