using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Transacoes;

public sealed class LancamentoExtratoTests
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
        LancamentoExtrato result = LancamentoExtrato.Criar(data, hora: null, valor, descricao, contraparte, identidade);

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
        LancamentoExtrato result = LancamentoExtrato.Criar(data, hora: null, valor, descricao, $" {contraparte} ", identidade);

        result.Contraparte.Should().Be(contraparte);
    }
}
