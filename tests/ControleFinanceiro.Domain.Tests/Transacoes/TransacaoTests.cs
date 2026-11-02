using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Transacoes;

public sealed class TransacaoTests
{
    [Theory, TransacaoAutoData]
    public void EhOrigemConta_Quando_DaConta_Deve_SerVerdadeiro(TransacoesDeTeste transacoes, Lancamento lancamento)
    {
        Transacao transacao = transacoes.DaConta(lancamento.SemCategoria());

        transacao.EhOrigemConta.Should().BeTrue();
    }

    [Theory, TransacaoAutoData]
    public void EhOrigemConta_Quando_DoCartao_Deve_SerFalso(TransacoesDeTeste transacoes, Cartao cartao, Lancamento lancamento)
    {
        Transacao transacao = transacoes.DoCartao(cartao, lancamento.SemCategoria());

        transacao.EhOrigemConta.Should().BeFalse();
    }

    /// <summary>Tudo sai do mesmo usuário.</summary>
    private sealed class TransacaoAutoDataAttribute : DomainAutoDataAttribute
    {
        public TransacaoAutoDataAttribute()
            : base(fixture => fixture.Freeze<IdUsuario>())
        {
        }
    }
}
