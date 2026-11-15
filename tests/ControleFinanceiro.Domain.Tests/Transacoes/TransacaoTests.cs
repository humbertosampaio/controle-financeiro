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

    [Theory, TransacaoAutoData]
    public void EhPagamentoFatura_Quando_LancamentoDePagamentoFatura_Deve_SerVerdadeiro(TransacoesDeTeste transacoes, Cartao cartao, Lancamento lancamento)
    {
        Transacao transacao = transacoes.DoCartao(cartao, lancamento.SemCategoria().ComoPagamentoFatura());

        transacao.EhPagamentoFatura.Should().BeTrue();
    }

    [Theory, TransacaoAutoData]
    public void EhPagamentoFatura_Quando_CompraNoCartao_Deve_SerFalso(TransacoesDeTeste transacoes, Cartao cartao, Lancamento lancamento)
    {
        Transacao transacao = transacoes.DoCartao(cartao, lancamento.SemCategoria().ComoSaida());

        transacao.EhPagamentoFatura.Should().BeFalse();
    }

    [Theory, TransacaoAutoData]
    public void EhCompraAVistaNoCartao_Quando_PagamentoFatura_Deve_SerFalso(TransacoesDeTeste transacoes, Cartao cartao, Lancamento lancamento)
    {
        Transacao transacao = transacoes.DoCartao(cartao, lancamento.SemCategoria().ComoPagamentoFatura());

        transacao.EhCompraAVistaNoCartao.Should().BeFalse();
    }

    [Theory, TransacaoAutoData]
    public void CompoeTotalFatura_Quando_CompraNoCartaoNoMesDaFatura_Deve_SerVerdadeiro(TransacoesDeTeste transacoes, Cartao cartao, Lancamento lancamento)
    {
        Transacao transacao = transacoes.DoCartao(cartao, lancamento.SemCategoria().ComoSaida());

        transacao.CompoeTotalFatura(FaturaTransacao(cartao, transacao)).Should().BeTrue();
    }

    [Theory, TransacaoAutoData]
    public void CompoeTotalFatura_Quando_EstornoNoCartao_Deve_SerVerdadeiro(TransacoesDeTeste transacoes, Cartao cartao, Lancamento lancamento)
    {
        Transacao transacao = transacoes.DoCartao(cartao, lancamento.SemCategoria().ComoEntrada());

        transacao.CompoeTotalFatura(FaturaTransacao(cartao, transacao)).Should().BeTrue();
    }

    [Theory, TransacaoAutoData]
    public void CompoeTotalFatura_Quando_FaturaDeOutroMes_Deve_SerFalso(TransacoesDeTeste transacoes, Cartao cartao, Lancamento lancamento)
    {
        Transacao transacao = transacoes.DoCartao(cartao, lancamento.SemCategoria().ComoSaida());
        FaturaCartao faturaMesSeguinte = FaturaCartao.DoCartao(cartao.Id, [cartao], transacao.MesVencimentoFatura!.Posterior());

        transacao.CompoeTotalFatura(faturaMesSeguinte).Should().BeFalse();
    }

    [Theory, TransacaoAutoData]
    public void CompoeTotalFatura_Quando_FaturaDeOutroCartao_Deve_SerFalso(TransacoesDeTeste transacoes, Cartao cartao, Lancamento lancamento)
    {
        Transacao transacao = transacoes.DoCartao(cartao, lancamento.SemCategoria().ComoSaida());

        transacao.CompoeTotalFatura(FaturaTransacao(cartao.EmOutroBanco(), transacao)).Should().BeFalse();
    }

    [Theory, TransacaoAutoData]
    public void CompoeTotalFatura_Quando_PagamentoFaturaAnterior_Deve_SerFalso(TransacoesDeTeste transacoes, Cartao cartao, Lancamento lancamento)
    {
        Transacao transacao = transacoes.DoCartao(cartao, lancamento.SemCategoria().ComoPagamentoFatura());

        transacao.CompoeTotalFatura(FaturaTransacao(cartao, transacao)).Should().BeFalse();
    }

    [Theory, TransacaoAutoData]
    public void CompoeTotalFatura_Quando_DaConta_Deve_SerFalso(TransacoesDeTeste transacoes, Cartao cartao, Lancamento lancamento, MesReferencia mesVencimento)
    {
        Transacao transacao = transacoes.DaConta(lancamento.SemCategoria().ComoSaida());

        transacao.CompoeTotalFatura(FaturaCartao.DoCartao(cartao.Id, [cartao], mesVencimento)).Should().BeFalse();
    }

    [Theory, TransacaoAutoData]
    public void EhSaidaConta_Quando_SaidaDaConta_Deve_SerVerdadeiro(TransacoesDeTeste transacoes, Lancamento lancamento)
    {
        Transacao transacao = transacoes.DaConta(lancamento.SemCategoria().ComoSaida());

        transacao.EhSaidaConta.Should().BeTrue();
    }

    [Theory, TransacaoAutoData]
    public void EhSaidaConta_Quando_EntradaNaConta_Deve_SerFalso(TransacoesDeTeste transacoes, Lancamento lancamento)
    {
        Transacao transacao = transacoes.DaConta(lancamento.SemCategoria().ComoEntrada());

        transacao.EhSaidaConta.Should().BeFalse();
    }

    [Theory, TransacaoAutoData]
    public void EhSaidaConta_Quando_SaidaNoCartao_Deve_SerFalso(TransacoesDeTeste transacoes, Cartao cartao, Lancamento lancamento)
    {
        Transacao transacao = transacoes.DoCartao(cartao, lancamento.SemCategoria().ComoSaida());

        transacao.EhSaidaConta.Should().BeFalse();
    }

    /// <summary>A fatura do <paramref name="cartao"/> no mês em que vence a fatura que trouxe a transação.</summary>
    private static FaturaCartao FaturaTransacao(Cartao cartao, Transacao transacao) =>
        FaturaCartao.DoCartao(cartao.Id, [cartao], transacao.MesVencimentoFatura!);

    /// <summary>Tudo sai do mesmo usuário.</summary>
    private sealed class TransacaoAutoDataAttribute : DomainAutoDataAttribute
    {
        public TransacaoAutoDataAttribute()
            : base(fixture => fixture.Freeze<IdUsuario>())
        {
        }
    }
}
