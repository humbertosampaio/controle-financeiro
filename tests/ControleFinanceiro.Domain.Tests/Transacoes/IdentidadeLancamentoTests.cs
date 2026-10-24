using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Transacoes;

public sealed class IdentidadeLancamentoTests
{
    private static readonly DateOnly _dia = new(2026, 03, 05);

    [Fact]
    public void Composta_Deve_ComecarPelaData()
    {
        IdentidadeLancamento result = IdentidadeLancamento.Composta(_dia);

        result.Campos.Should().Equal("2026-03-05");
        result.EhUnica.Should().BeFalse();
    }

    [Fact]
    public void Composta_Quando_VariosCampos_Deve_ManterAOrdemDeclarada()
    {
        IdentidadeLancamento result = IdentidadeLancamento.Composta(_dia)
            .ComValor(-123.4m)
            .ComHora(new TimeOnly(14, 30))
            .ComTexto("padaria");

        result.Campos.Should().Equal("2026-03-05", "-123.40", "14:30:00", "PADARIA");
    }

    [Theory, DomainAutoData]
    public void ComValor_Deve_ManterAOriginalSemOCampo(DateOnly data, Reais valor)
    {
        IdentidadeLancamento original = IdentidadeLancamento.Composta(data);

        original.ComValor(valor);

        original.Campos.Should().ContainSingle();
    }

    [Theory, DomainAutoData]
    public void ComHora_Quando_SemHora_Deve_AdicionarCampoVazio(DateOnly data)
    {
        IdentidadeLancamento result = IdentidadeLancamento.Composta(data).ComHora(null);

        result.Campos[^1].Should().BeEmpty();
    }

    [Theory]
    [InlineDomainAutoData(1.2, "1.20")]
    [InlineDomainAutoData(0, "0.00")]
    [InlineDomainAutoData(-1234567.89, "-1234567.89")]
    public void ComValor_Deve_FormatarComDuasCasasEPonto(decimal valor, string esperado, DateOnly data)
    {
        IdentidadeLancamento result = IdentidadeLancamento.Composta(data).ComValor(valor);

        result.Campos[^1].Should().Be(esperado);
    }

    [Theory]
    [InlineDomainAutoData("  padaria   são  joão ", "PADARIA SÃO JOÃO")]
    [InlineDomainAutoData("João", "JOÃO")]
    [InlineDomainAutoData("linha\tcom\nquebras", "LINHA COM QUEBRAS")]
    public void ComTexto_Deve_Normalizar(string texto, string esperado, DateOnly data)
    {
        IdentidadeLancamento result = IdentidadeLancamento.Composta(data).ComTexto(texto);

        result.Campos[^1].Should().Be(esperado);
    }

    [Theory]
    [InlineDomainAutoData(null)]
    [InlineDomainAutoData("")]
    [InlineDomainAutoData("   ")]
    public void ComTexto_Quando_EmBranco_Deve_AdicionarCampoVazio(string? texto, DateOnly data)
    {
        IdentidadeLancamento result = IdentidadeLancamento.Composta(data).ComTexto(texto);

        result.Campos[^1].Should().BeEmpty();
    }

    [Fact]
    public void ComParcela_Deve_FormatarComoNumeroBarraTotal()
    {
        IdentidadeLancamento result = IdentidadeLancamento.Composta(_dia).ComParcela((Parcela)(3, 10));

        result.Campos[^1].Should().Be("3/10");
    }

    [Theory, DomainAutoData]
    public void ComParcela_Quando_SemParcela_Deve_AdicionarCampoVazio(DateOnly data)
    {
        IdentidadeLancamento result = IdentidadeLancamento.Composta(data).ComParcela(null);

        result.Campos[^1].Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public void Equals_Quando_ParcelasDiferentes_Deve_SerDiferente(DateOnly data, Reais valor)
    {
        IdentidadeLancamento terceira = IdentidadeLancamento.Composta(data).ComValor(valor).ComParcela((Parcela)(3, 10));

        IdentidadeLancamento quarta = IdentidadeLancamento.Composta(data).ComValor(valor).ComParcela((Parcela)(4, 10));

        quarta.Should().NotBe(terceira);
    }

    [Fact]
    public void Unica_Deve_ManterOIdComoOBancoEscreveuSoAparado()
    {
        Result<IdentidadeLancamento> result = IdentidadeLancamento.Unica(" aB-123 ");

        IdentidadeLancamento identidade = result.Should().Succeed().Subject;
        identidade.Campos.Should().Equal("aB-123");
        identidade.EhUnica.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Unica_Quando_IdEmBranco_Deve_RetornarFalha(string? idExterno)
    {
        Result<IdentidadeLancamento> result = IdentidadeLancamento.Unica(idExterno);

        result.Should().FailWith("O identificador do lançamento no banco está em branco.");
    }

    [Theory, DomainAutoData]
    public void ComHora_Quando_IdentidadeUnica_Deve_LancarExcecao(IdentidadeLancamento unica, TimeOnly hora)
    {
        Func<IdentidadeLancamento> act = () => unica.ComHora(hora);

        act.Should().ThrowExactly<InvalidOperationException>();
    }

    [Theory, DomainAutoData]
    public void Equals_Quando_MesmosCamposNaMesmaOrdem_Deve_SerIgual(DateOnly data, Reais valor)
    {
        IdentidadeLancamento result = IdentidadeLancamento.Composta(data).ComValor(valor);

        result.Should().Be(IdentidadeLancamento.Composta(data).ComValor(valor));
    }
}
