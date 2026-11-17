using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Transacoes;

public sealed class TransacaoContaTests
{
    [Theory, TransacaoAutoData]
    public void TextoBeneficiario_Quando_Saida_Deve_SerAContraparte(TransacoesDeTeste transacoes, LancamentoConta lancamento, string contraparte)
    {
        TransacaoConta transacao = transacoes.DaConta(lancamento.SemCategoria().ComoSaida().ComTextos(lancamento.Descricao, contraparte));

        transacao.TextoBeneficiario.Should().Be(contraparte);
    }

    [Theory, TransacaoAutoData]
    public void TextoBeneficiario_Quando_SaidaSemContraparte_Deve_SerNulo(TransacoesDeTeste transacoes, LancamentoConta lancamento)
    {
        TransacaoConta transacao = transacoes.DaConta(lancamento.SemCategoria().ComoSaida().ComTextos(lancamento.Descricao, contraparte: null));

        transacao.TextoBeneficiario.Should().BeNull();
    }

    [Theory, TransacaoAutoData]
    public void TextoBeneficiario_Quando_Entrada_Deve_SerNulo(TransacoesDeTeste transacoes, LancamentoConta lancamento)
    {
        TransacaoConta transacao = transacoes.DaConta(lancamento.SemCategoria().ComoEntrada());

        transacao.TextoBeneficiario.Should().BeNull();
    }

    [Theory, TransacaoAutoData]
    public void AliasTextoBeneficiario_Deve_SerODoLancamento(TransacoesDeTeste transacoes, LancamentoConta lancamento)
    {
        LancamentoConta saida = lancamento.SemCategoria().ComoSaida();

        TransacaoConta transacao = transacoes.DaConta(saida);

        transacao.AliasTextoBeneficiario.Should().Be(saida.AliasTextoBeneficiario);
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
