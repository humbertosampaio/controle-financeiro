using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
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

    [Theory, TransacaoAutoData]
    public void EhCompraAVistaNoCartao_Quando_SaidaSemParcelaNoCartao_Deve_SerVerdadeiro(TransacoesDeTeste transacoes, Cartao cartao, Lancamento lancamento)
    {
        Transacao transacao = transacoes.DoCartao(cartao, lancamento.SemCategoria().ComoSaida());

        transacao.EhCompraAVistaNoCartao.Should().BeTrue();
    }

    [Theory, TransacaoAutoData]
    public void EhCompraAVistaNoCartao_Quando_Parcelada_Deve_SerFalso(TransacoesDeTeste transacoes, Cartao cartao, Lancamento lancamento, Parcela parcela)
    {
        Lancamento compra = Lancamento.DoCartao(
            lancamento.Data,
            lancamento.Hora,
            -(decimal.Abs(lancamento.Valor) + 1m),
            lancamento.Descricao,
            lancamento.Contraparte,
            parcela,
            categoriaBanco: null,
            lancamento.Identidade);

        Transacao transacao = transacoes.DoCartao(cartao, compra);

        transacao.EhCompraAVistaNoCartao.Should().BeFalse();
    }

    [Theory, TransacaoAutoData]
    public void EhCompraAVistaNoCartao_Quando_SaidaDaConta_Deve_SerFalso(TransacoesDeTeste transacoes, Lancamento lancamento)
    {
        Transacao transacao = transacoes.DaConta(lancamento.SemCategoria().ComoSaida());

        transacao.EhCompraAVistaNoCartao.Should().BeFalse();
    }

    [Theory, TransacaoAutoData]
    public void EhCompraAVistaNoCartao_Quando_EntradaNoCartao_Deve_SerFalso(TransacoesDeTeste transacoes, Cartao cartao, Lancamento lancamento)
    {
        Lancamento entrada = Lancamento.DoCartao(
            lancamento.Data,
            lancamento.Hora,
            decimal.Abs(lancamento.Valor) + 1m,
            lancamento.Descricao,
            lancamento.Contraparte,
            parcela: null,
            categoriaBanco: null,
            lancamento.Identidade);

        Transacao transacao = transacoes.DoCartao(cartao, entrada);

        transacao.EhCompraAVistaNoCartao.Should().BeFalse();
    }

    [Theory, TransacaoAutoData]
    public void EhCompraAVistaNoCartao_Quando_DescricaoNormalizaParaVazio_Deve_SerFalso(TransacoesDeTeste transacoes, Cartao cartao, Lancamento lancamento)
    {
        Lancamento semTexto = lancamento.SemCategoria().ComoSaida().ComTextos((TextoObrigatorio)"\u0301", contraparte: null);

        Transacao transacao = transacoes.DoCartao(cartao, semTexto);

        transacao.EhCompraAVistaNoCartao.Should().BeFalse();
    }

    [Theory, TransacaoAutoData]
    public void AliasTextoBeneficiario_Deve_SerODoLancamento(TransacoesDeTeste transacoes, Cartao cartao, Lancamento lancamento)
    {
        Lancamento compra = lancamento.SemCategoria().ComoSaida().NaOrigem(TipoOrigem.Cartao);

        Transacao transacao = transacoes.DoCartao(cartao, compra);

        transacao.AliasTextoBeneficiario.Should().Be(compra.AliasTextoBeneficiario);
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
