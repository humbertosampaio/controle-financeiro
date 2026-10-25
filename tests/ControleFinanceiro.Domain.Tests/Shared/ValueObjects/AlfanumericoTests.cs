using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Tests.Shared.ValueObjects;

public sealed class AlfanumericoTests
{
    [Theory]
    [InlineData("123.456.789-09", "12345678909")]
    [InlineData("12.abc.345/01DE-35", "12abc34501DE35")]
    [InlineData(" 12_34,5\t", "12345")]
    [InlineData("ÇÃ1é٢", "1")]
    [InlineData("", "")]
    public void Criar_Deve_ManterSoAsLetrasEOsDigitosAsciiNaMesmaCaixa(string texto, string esperado)
    {
        Alfanumerico result = Alfanumerico.Criar(texto);

        result.Value.Should().Be(esperado);
    }

    [Fact]
    public void Criar_Quando_Nulo_Deve_RetornarTextoVazio()
    {
        Alfanumerico result = Alfanumerico.Criar(texto: null);

        result.Value.Should().BeEmpty();
    }
}
