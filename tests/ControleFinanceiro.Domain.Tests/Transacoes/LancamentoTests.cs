using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Transacoes;

public sealed class LancamentoTests
{
    [Theory]
    [InlineDomainAutoData(null)]
    [InlineDomainAutoData("")]
    [InlineDomainAutoData("   ")]
    public void DaConta_Quando_ContraparteEmBranco_Deve_DeixarNula(
        string? contraparte,
        DateOnly data,
        Reais valor,
        TextoObrigatorio descricao,
        IdentidadeLancamento identidade)
    {
        Lancamento result = Lancamento.DaConta(data, hora: null, valor, descricao, contraparte, categoriaBanco: null, identidade);

        result.Contraparte.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void DaConta_Quando_ContraparteComEspacosNasPontas_Deve_Aparar(
        DateOnly data,
        Reais valor,
        TextoObrigatorio descricao,
        string contraparte,
        IdentidadeLancamento identidade)
    {
        Lancamento result = Lancamento.DaConta(data, hora: null, valor, descricao, $" {contraparte} ", categoriaBanco: null, identidade);

        result.Contraparte.Should().Be(contraparte);
    }

    [Theory]
    [InlineDomainAutoData(null)]
    [InlineDomainAutoData("")]
    [InlineDomainAutoData("   ")]
    public void DaConta_Quando_CategoriaBancoEmBranco_Deve_DeixarNula(
        string? categoriaBanco,
        DateOnly data,
        Reais valor,
        TextoObrigatorio descricao,
        IdentidadeLancamento identidade)
    {
        Lancamento result = Lancamento.DaConta(data, hora: null, valor, descricao, contraparte: null, categoriaBanco, identidade);

        result.CategoriaBanco.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void DaConta_Quando_CategoriaBancoComEspacosNasPontas_Deve_Aparar(
        DateOnly data,
        Reais valor,
        TextoObrigatorio descricao,
        string categoriaBanco,
        IdentidadeLancamento identidade)
    {
        Lancamento result = Lancamento.DaConta(data, hora: null, valor, descricao, contraparte: null, $" {categoriaBanco} ", identidade);

        result.CategoriaBanco.Should().Be(categoriaBanco);
    }

    [Theory, DomainAutoData]
    public void DoCartao_Quando_ContraparteECategoriaComEspacosNasPontas_Deve_Aparar(
        DateOnly data,
        Reais valor,
        TextoObrigatorio descricao,
        string contraparte,
        string categoriaBanco,
        IdentidadeLancamento identidade)
    {
        Lancamento result = Lancamento.DoCartao(data, hora: null, valor, descricao, $" {contraparte} ", parcela: null, $" {categoriaBanco} ", identidade);

        result.Contraparte.Should().Be(contraparte);
        result.CategoriaBanco.Should().Be(categoriaBanco);
    }

    [Theory, DomainAutoData]
    public void DoCartao_Deve_GuardarAParcela(
        DateOnly data,
        Reais valor,
        TextoObrigatorio descricao,
        Parcela parcela,
        IdentidadeLancamento identidade)
    {
        Lancamento result = Lancamento.DoCartao(data, hora: null, valor, descricao, contraparte: null, parcela, categoriaBanco: null, identidade);

        result.Parcela.Should().Be(parcela);
    }

    [Theory, DomainAutoData]
    public void Equals_Quando_MesmosCampos_Deve_SerIgual(
        DateOnly data,
        TimeOnly hora,
        Reais valor,
        TextoObrigatorio descricao,
        string contraparte,
        Parcela parcela,
        string categoriaBanco,
        IdentidadeLancamento identidade)
    {
        Lancamento lancamento = Lancamento.DoCartao(data, hora, valor, descricao, contraparte, parcela, categoriaBanco, identidade);

        Lancamento outro = Lancamento.DoCartao(data, hora, valor, descricao, contraparte, parcela, categoriaBanco, identidade);

        outro.Should().Be(lancamento);
    }

    [Theory]
    [InlineDomainAutoData(-0.01, true)]
    [InlineDomainAutoData(0, false)]
    [InlineDomainAutoData(0.01, false)]
    public void EhSaida_Deve_SerVerdadeiroSoComValorNegativo(
        decimal valor,
        bool esperado,
        DateOnly data,
        TextoObrigatorio descricao,
        IdentidadeLancamento identidade)
    {
        Lancamento lancamento = Lancamento.DaConta(data, hora: null, valor, descricao, contraparte: null, categoriaBanco: null, identidade);

        lancamento.EhSaida.Should().Be(esperado);
    }

    [Theory]
    [InlineDomainAutoData(-0.01, false)]
    [InlineDomainAutoData(0, false)]
    [InlineDomainAutoData(0.01, true)]
    public void EhEntrada_Deve_SerVerdadeiroSoComValorPositivo(
        decimal valor,
        bool esperado,
        DateOnly data,
        TextoObrigatorio descricao,
        IdentidadeLancamento identidade)
    {
        Lancamento lancamento = Lancamento.DaConta(data, hora: null, valor, descricao, contraparte: null, categoriaBanco: null, identidade);

        lancamento.EhEntrada.Should().Be(esperado);
    }

    [Theory, DomainAutoData]
    public void TextoBeneficiario_Quando_ValorZeroNaConta_Deve_RetornarNulo(
        DateOnly data,
        TextoObrigatorio descricao,
        string contraparte,
        IdentidadeLancamento identidade)
    {
        Lancamento lancamento = Lancamento.DaConta(data, hora: null, valor: 0m, descricao, contraparte, categoriaBanco: null, identidade);

        lancamento.TextoBeneficiario.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void TextoBeneficiario_Quando_ValorZeroNoCartao_Deve_RetornarNulo(
        DateOnly data,
        TextoObrigatorio descricao,
        string contraparte,
        IdentidadeLancamento identidade)
    {
        Lancamento lancamento = Lancamento.DoCartao(data, hora: null, valor: 0m, descricao, contraparte, parcela: null, categoriaBanco: null, identidade);

        lancamento.TextoBeneficiario.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void TextoBeneficiario_Quando_SaidaNaConta_Deve_RetornarAContraparte(
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        string contraparte,
        IdentidadeLancamento identidade)
    {
        Lancamento saida = Lancamento.DaConta(data, hora: null, -valor.Value, descricao, contraparte, categoriaBanco: null, identidade);

        saida.TextoBeneficiario.Should().Be(contraparte);
    }

    [Theory, DomainAutoData]
    public void TextoBeneficiario_Quando_SaidaNaContaSemContraparte_Deve_RetornarNulo(
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        IdentidadeLancamento identidade)
    {
        Lancamento boleto = Lancamento.DaConta(data, hora: null, -valor.Value, descricao, contraparte: null, categoriaBanco: null, identidade);

        boleto.TextoBeneficiario.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void TextoBeneficiario_Quando_SaidaNoCartao_Deve_RetornarADescricao(
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio estabelecimento,
        string contraparte,
        IdentidadeLancamento identidade)
    {
        Lancamento compra = Lancamento.DoCartao(data, hora: null, -valor.Value, estabelecimento, contraparte, parcela: null, categoriaBanco: null, identidade);

        compra.TextoBeneficiario.Should().Be(estabelecimento.Value);
    }

    [Theory, DomainAutoData]
    public void TextoBeneficiario_Quando_EntradaNaConta_Deve_RetornarNulo(
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        string contraparte,
        IdentidadeLancamento identidade)
    {
        Lancamento entrada = Lancamento.DaConta(data, hora: null, valor, descricao, contraparte, categoriaBanco: null, identidade);

        entrada.TextoBeneficiario.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void TextoBeneficiario_Quando_EntradaNoCartao_Deve_RetornarNulo(
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        string contraparte,
        IdentidadeLancamento identidade)
    {
        Lancamento entrada = Lancamento.DoCartao(data, hora: null, valor, descricao, contraparte, parcela: null, categoriaBanco: null, identidade);

        entrada.TextoBeneficiario.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void DaConta_Deve_TerOTipoDeOrigemConta(DateOnly data, Reais valor, TextoObrigatorio descricao, IdentidadeLancamento identidade)
    {
        Lancamento result = Lancamento.DaConta(data, hora: null, valor, descricao, contraparte: null, categoriaBanco: null, identidade);

        result.TipoOrigem.Should().Be(TipoOrigem.Conta);
    }

    [Theory, DomainAutoData]
    public void DoCartao_Deve_TerOTipoDeOrigemCartao(DateOnly data, Reais valor, TextoObrigatorio descricao, IdentidadeLancamento identidade)
    {
        Lancamento result = Lancamento.DoCartao(data, hora: null, valor, descricao, contraparte: null, parcela: null, categoriaBanco: null, identidade);

        result.TipoOrigem.Should().Be(TipoOrigem.Cartao);
    }

    [Theory, DomainAutoData]
    public void Equals_Quando_OutroTipoDeOrigem_Deve_SerDiferente(Lancamento lancamento)
    {
        Lancamento noCartao = lancamento.NaOrigem(TipoOrigem.Cartao);

        noCartao.Should().NotBe(lancamento);
    }

}
