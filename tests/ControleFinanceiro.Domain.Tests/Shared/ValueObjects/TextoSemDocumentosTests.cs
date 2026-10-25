using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Tests.Shared.ValueObjects;

/// <summary>Os documentos são os exemplos de teste conhecidos do CPF e do CNPJ.</summary>
public sealed class TextoSemDocumentosTests
{
    [Theory]
    [InlineData("FULANO FICTICIO 12345678909", "FULANO FICTICIO")]
    [InlineData("FULANO FICTICIO 123.456.789-09", "FULANO FICTICIO")]
    [InlineData("FULANO FICTICIO12345678909", "FULANO FICTICIO")]
    [InlineData("FULANO FICTICIO 123 456 789 09", "FULANO FICTICIO")]
    [InlineData("FULANO FICTICIO ***.456.789-**", "FULANO FICTICIO")]
    [InlineData("FULANO FICTICIO ***456789**", "FULANO FICTICIO")]
    [InlineData("LOJA FICTICIA 11.222.333/0001-81", "LOJA FICTICIA")]
    [InlineData("LOJA FICTICIA 11222333000181", "LOJA FICTICIA")]
    [InlineData("LOJA FICTICIA11222333000181", "LOJA FICTICIA")]
    [InlineData("LOJA FICTICIA 11 222 333 0001 81", "LOJA FICTICIA")]
    [InlineData("LOJA FICTICIA 12.ABC.345/01DE-35", "LOJA FICTICIA")]
    [InlineData("LOJA FICTICIA 12abc34501de35", "LOJA FICTICIA")]
    [InlineData("123.456.789-09 LOJA FICTICIA 11.222.333/0001-81", "LOJA FICTICIA")]
    public void Criar_Quando_DocumentoNoTexto_Deve_TirarODocumentoEOEspacoAntes(string texto, string esperado)
    {
        TextoSemDocumentos result = TextoSemDocumentos.Criar(texto);

        result.Value.Should().Be(esperado);
    }

    [Theory]
    [InlineData("FULANO FICTICIO 12345678900")]
    [InlineData("LOJA FICTICIA 11222333000182")]
    [InlineData("PEDIDO 1234567890912")]
    public void Criar_Quando_NumeroQueNaoEDocumento_Deve_ManterONumero(string texto)
    {
        TextoSemDocumentos result = TextoSemDocumentos.Criar(texto);

        result.Value.Should().Be(texto);
    }

    [Theory]
    [InlineData(" LOJA FICTICIA ", "LOJA FICTICIA")]
    [InlineData(" 123.456.789-09 ", "")]
    [InlineData("", "")]
    public void Criar_Deve_ApararOTexto(string texto, string esperado)
    {
        TextoSemDocumentos result = TextoSemDocumentos.Criar(texto);

        result.Value.Should().Be(esperado);
    }

    [Fact]
    public void Criar_Quando_Nulo_Deve_RetornarTextoVazio()
    {
        TextoSemDocumentos result = TextoSemDocumentos.Criar(texto: null);

        result.Value.Should().BeEmpty();
    }

    [Fact]
    public void OperadorExplicito_Deve_TirarOsDocumentosComoOCriar()
    {
        TextoSemDocumentos result = (TextoSemDocumentos)"LOJA FICTICIA 11.222.333/0001-81";

        result.Should().Be(TextoSemDocumentos.Criar("LOJA FICTICIA"));
    }
}
