using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Shared.ValueObjects;

public sealed class AliasTests
{
    [Theory, MemberData(nameof(ValoresVazios))]
    public void Criar_Quando_ValorVazio_Deve_RetornarFalha(string? valor)
    {
        Result<Alias> result = Alias.Criar(valor);

        result.Should().FailWith("Alias é obrigatório");
    }

    [Theory]
    [InlineData("Padaria São João", "PADARIA SAO JOAO")]
    [InlineData("  padaria   sao\tjoao ", "PADARIA SAO JOAO")]
    [InlineData("AÇOUGUE BÜRGER", "ACOUGUE BURGER")]
    [InlineData("José", "JOSE")]
    public void Criar_Deve_NormalizarSemAcentosEspacosRepetidosECaixa(string texto, string esperado)
    {
        Result<Alias> result = Alias.Criar(texto);

        result.Should().Succeed()
            .Which.TextoNormalizado.Should().Be(esperado);
    }

    [Theory, AutoData]
    public void Criar_Quando_EspacosNasPontas_Deve_GuardarOTextoOriginalAparado(string valor)
    {
        Result<Alias> result = Alias.Criar($"  {valor}\t ");

        result.Should().Succeed()
            .Which.TextoOriginal.Should().Be(valor);
    }

    [Fact]
    public void Criar_Quando_AcentoComposto_Deve_NormalizarIgualAoAcentoPrecomposto()
    {
        const string textoDecomposto = "Jose\u0301";
        const string textoPrecomposto = "Jos\u00e9";
        textoDecomposto.Should().NotBe(textoPrecomposto);

        Alias decomposto = (Alias)textoDecomposto;

        decomposto.Should().Be((Alias)textoPrecomposto);
    }

    [Fact]
    public void Igualdade_Quando_SoONormalizadoCoincide_Deve_SerIgual()
    {
        Alias alias = (Alias)"Padaria São João";

        Alias outro = (Alias)" PADARIA  SAO JOAO";

        alias.Should().Be(outro);
        alias.TextoOriginal.Should().NotBe(outro.TextoOriginal);
    }

    [Theory, MemberData(nameof(ValoresVazios))]
    public void OperadorExplicito_Quando_ValorVazio_Deve_LancarExcecao(string? valor)
    {
        Func<Alias> result = () => (Alias)valor;

        result.Should().ThrowExactly<ResultFailureException>();
    }

    [Theory, AutoData]
    public void OperadorExplicito_Quando_ValorValido_Deve_RetornarOMesmoQueOCriar(string valor)
    {
        Alias result = (Alias)valor;

        result.Should().Be(Alias.Criar(valor).Value);
    }

    public static readonly TheoryData<string?> ValoresVazios = new(string.Empty, null, "   ", "\t", "\u0301");
}
