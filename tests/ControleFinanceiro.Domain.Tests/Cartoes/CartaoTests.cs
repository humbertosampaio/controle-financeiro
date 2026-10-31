using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Cartoes;

public sealed class CartaoTests
{
    [Theory, DomainAutoData]
    public void AlterarCicloFatura_Deve_TrocarOsDiasDeVencimentoEDeFechamento(
        Cartao sut,
        DiaVencimentoCartao outroVencimento,
        DiaFechamentoCartao outroFechamento)
    {
        sut.AlterarCicloFatura(outroVencimento, outroFechamento);

        sut.DiaVencimento.Should().Be(outroVencimento);
        sut.DiaFechamento.Should().Be(outroFechamento);
    }
}
