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
        Lancamento result = Lancamento.Criar(data, hora: null, valor, descricao, contraparte, identidade);

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
        Lancamento result = Lancamento.Criar(data, hora: null, valor, descricao, $" {contraparte} ", identidade);

        result.Contraparte.Should().Be(contraparte);
    }

    [Theory, DomainAutoData]
    public void Equals_Quando_MesmosCampos_Deve_SerIgual(
        DateOnly data,
        TimeOnly hora,
        Reais valor,
        TextoObrigatorio descricao,
        string contraparte,
        IdentidadeLancamento identidade)
    {
        Lancamento lancamento = Lancamento.Criar(data, hora, valor, descricao, contraparte, identidade);

        Lancamento outro = Lancamento.Criar(data, hora, valor, descricao, contraparte, identidade);

        outro.Should().Be(lancamento);
    }
}
