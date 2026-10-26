using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Transacoes;

public sealed class OrigemTransacaoTests
{
    [Theory, DomainAutoData]
    public void DaConta_Deve_TerSoAConta(IdConta idConta)
    {
        OrigemTransacao result = OrigemTransacao.DaConta(idConta);

        result.IdConta.Should().Be(idConta);
        result.IdCartao.Should().BeNull();
        result.Tipo.Should().Be(TipoOrigem.Conta);
    }

    [Theory, DomainAutoData]
    public void DoCartao_Deve_TerSoOCartao(IdCartao idCartao)
    {
        OrigemTransacao result = OrigemTransacao.DoCartao(idCartao);

        result.IdCartao.Should().Be(idCartao);
        result.IdConta.Should().BeNull();
        result.Tipo.Should().Be(TipoOrigem.Cartao);
    }

    [Theory, DomainAutoData]
    public void Equals_Quando_MesmaConta_Deve_SerIgual(IdConta idConta)
    {
        OrigemTransacao result = OrigemTransacao.DaConta(idConta);

        result.Should().Be(OrigemTransacao.DaConta(idConta));
    }
}
