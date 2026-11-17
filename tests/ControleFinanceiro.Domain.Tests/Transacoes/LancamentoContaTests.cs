using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Transacoes;

public sealed class LancamentoContaTests
{
    [Theory]
    [InlineDomainAutoData(null)]
    [InlineDomainAutoData("")]
    [InlineDomainAutoData("   ")]
    public void Criar_Quando_ContraparteEmBranco_Deve_DeixarNula(
        string? contraparte,
        DateOnly data,
        Reais valor,
        TextoObrigatorio descricao,
        IdentidadeLancamento identidade)
    {
        LancamentoConta result = LancamentoConta.Criar(data, hora: null, valor, descricao, contraparte, categoriaBanco: null, identidade);

        result.Contraparte.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_ContraparteComEspacosNasPontas_Deve_Aparar(
        DateOnly data,
        Reais valor,
        TextoObrigatorio descricao,
        string contraparte,
        IdentidadeLancamento identidade)
    {
        LancamentoConta result = LancamentoConta.Criar(data, hora: null, valor, descricao, $" {contraparte} ", categoriaBanco: null, identidade);

        result.Contraparte.Should().Be(contraparte);
    }

    [Theory]
    [InlineDomainAutoData(null)]
    [InlineDomainAutoData("")]
    [InlineDomainAutoData("   ")]
    public void Criar_Quando_CategoriaBancoEmBranco_Deve_DeixarNula(
        string? categoriaBanco,
        DateOnly data,
        Reais valor,
        TextoObrigatorio descricao,
        IdentidadeLancamento identidade)
    {
        LancamentoConta result = LancamentoConta.Criar(data, hora: null, valor, descricao, contraparte: null, categoriaBanco, identidade);

        result.CategoriaBanco.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_CategoriaBancoComEspacosNasPontas_Deve_Aparar(
        DateOnly data,
        Reais valor,
        TextoObrigatorio descricao,
        string categoriaBanco,
        IdentidadeLancamento identidade)
    {
        LancamentoConta result = LancamentoConta.Criar(data, hora: null, valor, descricao, contraparte: null, $" {categoriaBanco} ", identidade);

        result.CategoriaBanco.Should().Be(categoriaBanco);
    }

    [Theory, DomainAutoData]
    public void Equals_Quando_MesmosCampos_Deve_SerIgual(
        DateOnly data,
        TimeOnly hora,
        Reais valor,
        TextoObrigatorio descricao,
        string contraparte,
        string categoriaBanco,
        IdentidadeLancamento identidade)
    {
        LancamentoConta lancamento = LancamentoConta.Criar(data, hora, valor, descricao, contraparte, categoriaBanco, identidade);

        LancamentoConta outro = LancamentoConta.Criar(data, hora, valor, descricao, contraparte, categoriaBanco, identidade);

        outro.Should().Be(lancamento);
    }

    [Theory]
    [InlineDomainAutoData(-0.01, true)]
    [InlineDomainAutoData(0, false)]
    [InlineDomainAutoData(0.01, false)]
    public void EhSaida_Deve_SerVerdadeiroSoComValorNegativo(decimal valor, bool esperado, LancamentoConta lancamento)
    {
        LancamentoConta sut = ComValor(lancamento, valor);

        sut.EhSaida.Should().Be(esperado);
    }

    [Theory]
    [InlineDomainAutoData(-0.01, false)]
    [InlineDomainAutoData(0, false)]
    [InlineDomainAutoData(0.01, true)]
    public void EhEntrada_Deve_SerVerdadeiroSoComValorPositivo(decimal valor, bool esperado, LancamentoConta lancamento)
    {
        LancamentoConta sut = ComValor(lancamento, valor);

        sut.EhEntrada.Should().Be(esperado);
    }

    [Theory]
    [InlineDomainAutoData(-10.5, 10.5)]
    [InlineDomainAutoData(10.5, 10.5)]
    [InlineDomainAutoData(0, 0)]
    public void ValorAbsoluto_Deve_SerOValorSemOSinal(decimal valor, decimal esperado, LancamentoConta lancamento)
    {
        LancamentoConta sut = ComValor(lancamento, valor);

        sut.ValorAbsoluto.Should().Be((ReaisPositivo)esperado);
    }

    [Theory, DomainAutoData]
    public void TextoBeneficiario_Quando_Saida_Deve_RetornarAContraparte(LancamentoConta lancamento, ReaisPositivo valor, string contraparte)
    {
        LancamentoConta saida = ComValor(lancamento, -valor, contraparte);

        saida.TextoBeneficiario.Should().Be(contraparte);
    }

    [Theory, DomainAutoData]
    public void TextoBeneficiario_Quando_SaidaSemContraparte_Deve_RetornarNulo(LancamentoConta lancamento, ReaisPositivo valor)
    {
        LancamentoConta boleto = ComValor(lancamento, -valor, contraparte: null);

        boleto.TextoBeneficiario.Should().BeNull();
    }

    [Theory]
    [InlineDomainAutoData(10)]
    [InlineDomainAutoData(0)]
    public void TextoBeneficiario_Quando_EntradaOuValorZero_Deve_RetornarNulo(decimal valor, LancamentoConta lancamento, string contraparte)
    {
        LancamentoConta sut = ComValor(lancamento, valor, contraparte);

        sut.TextoBeneficiario.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void AliasTextoBeneficiario_Quando_Saida_Deve_RetornarOAliasDaContraparte(LancamentoConta lancamento, ReaisPositivo valor, string contraparte)
    {
        LancamentoConta saida = ComValor(lancamento, -valor, contraparte);

        saida.AliasTextoBeneficiario.Should().Be((Alias)contraparte);
    }

    [Theory, DomainAutoData]
    public void AliasTextoBeneficiario_Quando_SaidaSemContraparte_Deve_RetornarNulo(LancamentoConta lancamento, ReaisPositivo valor)
    {
        LancamentoConta boleto = ComValor(lancamento, -valor, contraparte: null);

        boleto.AliasTextoBeneficiario.Should().BeNull();
    }

    /// <summary>A mesma linha com o valor pedido e a contraparte do caso, sem categoria.</summary>
    private static LancamentoConta ComValor(LancamentoConta lancamento, Reais valor, string? contraparte = null)
    {
        return LancamentoConta.Criar(lancamento.Data, lancamento.Hora, valor, lancamento.Descricao, contraparte, categoriaBanco: null, lancamento.Identidade);
    }
}
