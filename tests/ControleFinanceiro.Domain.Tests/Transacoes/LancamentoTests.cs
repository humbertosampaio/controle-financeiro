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
}
