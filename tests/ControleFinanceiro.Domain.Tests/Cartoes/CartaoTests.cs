using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Cartoes;

public sealed class CartaoTests
{
    [Theory, DomainAutoData]
    public void AlterarVencimento_Deve_TrocarODiaDeVencimento(Cartao sut, DiaVencimentoCartao outroDia)
    {
        sut.AlterarVencimento(outroDia);

        sut.DiaVencimento.Should().Be(outroDia);
    }
}
