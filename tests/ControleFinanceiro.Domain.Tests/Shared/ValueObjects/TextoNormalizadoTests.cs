using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Tests.Shared.ValueObjects;

public sealed class TextoNormalizadoTests
{
    [Theory]
    [InlineData("Padaria São João", "PADARIA SAO JOAO")]
    [InlineData("  padaria   sao\tjoao ", "PADARIA SAO JOAO")]
    [InlineData("AÇOUGUE BÜRGER", "ACOUGUE BURGER")]
    public void Criar_Deve_NormalizarSemAcentosEspacosRepetidosECaixa(string texto, string esperado)
    {
        TextoNormalizado result = TextoNormalizado.Criar(texto);

        result.Value.Should().Be(esperado);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("́")]
    public void Criar_Quando_SemLetras_Deve_RetornarTextoVazio(string? texto)
    {
        TextoNormalizado result = TextoNormalizado.Criar(texto);

        result.Value.Should().BeEmpty();
    }

    [Fact]
    public void ConversaoTextoObrigatorio_Deve_NormalizarOTexto()
    {
        TextoNormalizado result = (TextoNormalizado)(TextoObrigatorio)"Saúde";

        result.Should().Be((TextoNormalizado)"SAUDE");
    }

    [Fact]
    public void ConversaoString_Deve_NormalizarOTexto()
    {
        TextoNormalizado result = (TextoNormalizado)" saúde ";

        result.Value.Should().Be("SAUDE");
    }
}
