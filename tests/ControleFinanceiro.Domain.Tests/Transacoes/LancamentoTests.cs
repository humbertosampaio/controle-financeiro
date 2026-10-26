using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Transacoes;

public sealed class LancamentoTests
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
        Lancamento result = Lancamento.Criar(data, hora: null, valor, descricao, contraparte, parcela: null, categoriaBanco: null, identidade);

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
        Lancamento result = Lancamento.Criar(data, hora: null, valor, descricao, $" {contraparte} ", parcela: null, categoriaBanco: null, identidade);

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
        Lancamento result = Lancamento.Criar(data, hora: null, valor, descricao, contraparte: null, parcela: null, categoriaBanco, identidade);

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
        Lancamento result = Lancamento.Criar(data, hora: null, valor, descricao, contraparte: null, parcela: null, $" {categoriaBanco} ", identidade);

        result.CategoriaBanco.Should().Be(categoriaBanco);
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_GuardarAParcela(
        DateOnly data,
        Reais valor,
        TextoObrigatorio descricao,
        Parcela parcela,
        IdentidadeLancamento identidade)
    {
        Lancamento result = Lancamento.Criar(data, hora: null, valor, descricao, contraparte: null, parcela, categoriaBanco: null, identidade);

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
        Lancamento lancamento = Lancamento.Criar(data, hora, valor, descricao, contraparte, parcela, categoriaBanco, identidade);

        Lancamento outro = Lancamento.Criar(data, hora, valor, descricao, contraparte, parcela, categoriaBanco, identidade);

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
        Lancamento lancamento = Lancamento.Criar(data, hora: null, valor, descricao, contraparte: null, parcela: null, categoriaBanco: null, identidade);

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
        Lancamento lancamento = Lancamento.Criar(data, hora: null, valor, descricao, contraparte: null, parcela: null, categoriaBanco: null, identidade);

        lancamento.EhEntrada.Should().Be(esperado);
    }

    [Theory]
    [InlineDomainAutoData(TipoOrigem.Conta)]
    [InlineDomainAutoData(TipoOrigem.Cartao)]
    public void TextoBeneficiario_Quando_ValorZero_Deve_RetornarNulo(
        TipoOrigem tipoOrigem,
        DateOnly data,
        TextoObrigatorio descricao,
        string contraparte,
        IdentidadeLancamento identidade)
    {
        Lancamento lancamento = Lancamento.Criar(data, hora: null, valor: 0m, descricao, contraparte, parcela: null, categoriaBanco: null, identidade);

        lancamento.TextoBeneficiario(tipoOrigem).Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void TextoBeneficiario_Quando_SaidaNaConta_Deve_RetornarAContraparte(
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        string contraparte,
        IdentidadeLancamento identidade)
    {
        Lancamento saida = Lancamento.Criar(data, hora: null, -valor.Value, descricao, contraparte, parcela: null, categoriaBanco: null, identidade);

        saida.TextoBeneficiario(TipoOrigem.Conta).Should().Be(contraparte);
    }

    [Theory, DomainAutoData]
    public void TextoBeneficiario_Quando_SaidaNaContaSemContraparte_Deve_RetornarNulo(
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        IdentidadeLancamento identidade)
    {
        Lancamento boleto = Lancamento.Criar(data, hora: null, -valor.Value, descricao, contraparte: null, parcela: null, categoriaBanco: null, identidade);

        boleto.TextoBeneficiario(TipoOrigem.Conta).Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void TextoBeneficiario_Quando_SaidaNoCartao_Deve_RetornarADescricao(
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio estabelecimento,
        string contraparte,
        IdentidadeLancamento identidade)
    {
        Lancamento compra = Lancamento.Criar(data, hora: null, -valor.Value, estabelecimento, contraparte, parcela: null, categoriaBanco: null, identidade);

        compra.TextoBeneficiario(TipoOrigem.Cartao).Should().Be(estabelecimento.Value);
    }

    [Theory]
    [InlineDomainAutoData(TipoOrigem.Conta)]
    [InlineDomainAutoData(TipoOrigem.Cartao)]
    public void TextoBeneficiario_Quando_Entrada_Deve_RetornarNulo(
        TipoOrigem tipoOrigem,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        string contraparte,
        IdentidadeLancamento identidade)
    {
        Lancamento entrada = Lancamento.Criar(data, hora: null, valor, descricao, contraparte, parcela: null, categoriaBanco: null, identidade);

        entrada.TextoBeneficiario(tipoOrigem).Should().BeNull();
    }
}
