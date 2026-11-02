using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Shared.ValueObjects;

public sealed class DocumentoMascaradoTests
{
    [Theory]
    [InlineDomainAutoData(FormaDocumento.CpfPuro)]
    [InlineDomainAutoData(FormaDocumento.CpfComPontuacao)]
    [InlineDomainAutoData(FormaDocumento.CpfComEspacos)]
    [InlineDomainAutoData(FormaDocumento.CnpjPuro)]
    [InlineDomainAutoData(FormaDocumento.CnpjComPontuacao)]
    [InlineDomainAutoData(FormaDocumento.CnpjComEspacos)]
    [InlineDomainAutoData(FormaDocumento.CnpjComLetras)]
    [InlineDomainAutoData(FormaDocumento.CnpjComLetrasComPontuacao)]
    public void Criar_Quando_DocumentoValido_Deve_TrocarPeloRotulo(FormaDocumento forma, DocumentosDeTeste documentos)
    {
        DocumentoMascarado result = DocumentoMascarado.Criar($"LOJA FICTICIA {documentos.Em(forma)}");

        result.Value.Should().Be($"LOJA FICTICIA {DocumentosDeTeste.Rotulo(forma)}");
    }

    [Theory]
    [InlineDomainAutoData(FormaDocumento.CpfPuro)]
    [InlineDomainAutoData(FormaDocumento.CnpjPuro)]
    public void Criar_Quando_DocumentoNumericoColadoALetras_Deve_TrocarPeloRotulo(FormaDocumento forma, DocumentosDeTeste documentos)
    {
        DocumentoMascarado result = DocumentoMascarado.Criar($"LOJA FICTICIA{documentos.Em(forma)}");

        result.Value.Should().Be($"LOJA FICTICIA{DocumentosDeTeste.Rotulo(forma)}");
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_CnpjComLetrasMinusculas_Deve_TrocarPeloRotulo(Cnpj cnpj)
    {
        DocumentoMascarado result = DocumentoMascarado.Criar($"LOJA FICTICIA {cnpj.Value.ToLowerInvariant()}");

        result.Value.Should().Be("LOJA FICTICIA [CNPJ]");
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_DoisDocumentos_Deve_TrocarOsDois(Cpf cpf, Cnpj cnpj)
    {
        DocumentoMascarado result = DocumentoMascarado.Criar($"{cpf} LOJA FICTICIA {cnpj}");

        result.Value.Should().Be("[CPF] LOJA FICTICIA [CNPJ]");
    }

    /// <summary>
    /// Documenta o risco aceito, listado no sumário da classe. Nenhum destes textos traz um documento válido, então os
    /// valores são o assunto do teste.
    /// </summary>
    [Theory]
    [InlineData("FULANO FICTICIO 12345678900")]
    [InlineData("LOJA FICTICIA 11222333000182")]
    [InlineData("LOJA FICTICIA 12.ABC.345/01DE-99")]
    [InlineData("FULANO FICTICIO ***.456.789-**")]
    [InlineData("PEDIDO 1234567890912")]
    [InlineData("CONTA 987654")]
    [InlineData("TELEFONE 11 91234-5678")]
    [InlineData("PIX fulano.ficticio@exemplo.com.br")]
    [InlineData("VENCIMENTO 05/03/2026")]
    [InlineData("AGENCIA 1234")]
    [InlineData("PARCELA 01/12")]
    [InlineData("MERCADOLIVRE22")]
    public void Criar_Quando_NaoEDocumentoValido_Deve_ManterOTexto(string texto)
    {
        DocumentoMascarado result = DocumentoMascarado.Criar(texto);

        result.Value.Should().Be(texto);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_DocumentoComSeparadorForaDaRegra_Deve_ManterOTexto(Cpf cpf)
    {
        string texto = $"FULANO FICTICIO {string.Join('_', cpf.Value.Chunk(3).Select(grupo => new string(grupo)))}";

        DocumentoMascarado result = DocumentoMascarado.Criar(texto);

        result.Value.Should().Be(texto);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_CnpjComLetrasColadoALetras_Deve_ManterOTexto(Cnpj cnpj)
    {
        string texto = $"LOJA{cnpj}";

        DocumentoMascarado result = DocumentoMascarado.Criar(texto);

        result.Value.Should().Be(texto);
    }

    [Theory]
    [InlineData(" LOJA FICTICIA ", "LOJA FICTICIA")]
    [InlineData("", "")]
    public void Criar_Deve_ApararOTexto(string texto, string esperado)
    {
        DocumentoMascarado result = DocumentoMascarado.Criar(texto);

        result.Value.Should().Be(esperado);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_DocumentoEntreEspacos_Deve_ApararOTexto(Cpf cpf)
    {
        DocumentoMascarado result = DocumentoMascarado.Criar($" {cpf} ");

        result.Value.Should().Be("[CPF]");
    }

    [Fact]
    public void Criar_Quando_Nulo_Deve_RetornarTextoVazio()
    {
        DocumentoMascarado result = DocumentoMascarado.Criar(texto: null);

        result.Value.Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public void OperadorExplicito_Deve_MascararComoOCriar(Cnpj cnpj)
    {
        DocumentoMascarado result = (DocumentoMascarado)$"LOJA FICTICIA {cnpj}";

        result.Value.Should().Be("LOJA FICTICIA [CNPJ]");
    }

    [Theory, DomainAutoData]
    public void OperadorExplicito_Quando_TextoObrigatorio_Deve_MascararComoOCriar(Cnpj cnpj)
    {
        TextoObrigatorio texto = (TextoObrigatorio)$"LOJA FICTICIA {cnpj}";

        DocumentoMascarado result = (DocumentoMascarado)texto;

        result.Value.Should().Be("LOJA FICTICIA [CNPJ]");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" - ")]
    public void TemConteudo_Quando_SoSeparadores_Deve_SerFalso(string texto)
    {
        DocumentoMascarado result = DocumentoMascarado.Criar(texto);

        result.TemConteudo.Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void TemConteudo_Quando_SoRotulos_Deve_SerFalso(Cpf cpf, Cnpj cnpj)
    {
        DocumentoMascarado result = DocumentoMascarado.Criar($"{cpf} - {cnpj}");

        result.TemConteudo.Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void TemConteudo_Quando_SobraTexto_Deve_SerVerdadeiro(Cpf cpf)
    {
        DocumentoMascarado result = DocumentoMascarado.Criar($"CPF: {cpf}");

        result.TemConteudo.Should().BeTrue();
    }

    [Theory, DomainAutoData]
    public void ConteudoOuVazio_Quando_SoRotulo_Deve_SerVazio(Cpf cpf)
    {
        DocumentoMascarado result = DocumentoMascarado.Criar(cpf);

        result.ConteudoOuVazio.Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public void ConteudoOuVazio_Quando_SobraTexto_Deve_ManterORotulo(Cpf cpf)
    {
        DocumentoMascarado result = DocumentoMascarado.Criar($"LOJA FICTICIA {cpf}");

        result.ConteudoOuVazio.Should().Be("LOJA FICTICIA [CPF]");
    }
}
