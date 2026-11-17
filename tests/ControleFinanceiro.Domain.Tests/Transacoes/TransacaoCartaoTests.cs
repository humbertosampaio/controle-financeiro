using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Transacoes;

public sealed class TransacaoCartaoTests
{
    [Theory, TransacaoAutoData]
    public void EhCompraAVista_Quando_SaidaSemParcelaNoCartao_Deve_SerVerdadeiro(TransacoesDeTeste transacoes, Cartao cartao, LancamentoConta lancamento)
    {
        TransacaoCartao transacao = transacoes.DoCartao(cartao, lancamento.SemCategoria().ComoSaida());

        transacao.EhCompraAVista.Should().BeTrue();
    }

    [Theory, TransacaoAutoData]
    public void EhCompraAVista_Quando_Parcelada_Deve_SerFalso(TransacoesDeTeste transacoes, Cartao cartao, LancamentoConta lancamento, Parcela parcela)
    {
        LancamentoCartao compra = LancamentoCartao.Criar(
            lancamento.Data,
            lancamento.Hora,
            -(decimal.Abs(lancamento.Valor) + 1m),
            lancamento.Descricao,
            lancamento.Contraparte,
            parcela,
            categoriaBanco: null,
            lancamento.Identidade);

        TransacaoCartao transacao = transacoes.DoCartao(cartao, compra);

        transacao.EhCompraAVista.Should().BeFalse();
    }

    [Theory, TransacaoAutoData]
    public void EhCompraAVista_Quando_EntradaNoCartao_Deve_SerFalso(TransacoesDeTeste transacoes, Cartao cartao, LancamentoConta lancamento)
    {
        LancamentoCartao entrada = LancamentoCartao.Criar(
            lancamento.Data,
            lancamento.Hora,
            decimal.Abs(lancamento.Valor) + 1m,
            lancamento.Descricao,
            lancamento.Contraparte,
            parcela: null,
            categoriaBanco: null,
            lancamento.Identidade);

        TransacaoCartao transacao = transacoes.DoCartao(cartao, entrada);

        transacao.EhCompraAVista.Should().BeFalse();
    }

    [Theory, TransacaoAutoData]
    public void EhCompraAVista_Quando_DescricaoNormalizaParaVazio_Deve_SerFalso(TransacoesDeTeste transacoes, Cartao cartao, LancamentoConta lancamento)
    {
        LancamentoConta semTexto = lancamento.SemCategoria().ComoSaida().ComTextos((TextoObrigatorio)"\u0301", contraparte: null);

        TransacaoCartao transacao = transacoes.DoCartao(cartao, semTexto);

        transacao.EhCompraAVista.Should().BeFalse();
    }

    [Theory, TransacaoAutoData]
    public void AliasTextoBeneficiario_Deve_SerODoLancamento(TransacoesDeTeste transacoes, Cartao cartao, LancamentoConta lancamento)
    {
        LancamentoCartao compra = lancamento.SemCategoria().ComoSaida().NoCartao();

        TransacaoCartao transacao = transacoes.DoCartao(cartao, compra);

        transacao.AliasTextoBeneficiario.Should().Be(compra.AliasTextoBeneficiario);
    }

    [Theory, TransacaoAutoData]
    public void EhPagamentoFaturaAnterior_Quando_LancamentoDePagamentoFatura_Deve_SerVerdadeiro(TransacoesDeTeste transacoes, Cartao cartao, LancamentoCartao lancamento)
    {
        TransacaoCartao transacao = transacoes.DoCartao(cartao, lancamento.SemCategoria().ComoPagamentoFaturaAnterior());

        transacao.EhPagamentoFaturaAnterior.Should().BeTrue();
    }

    [Theory, TransacaoAutoData]
    public void EhPagamentoFaturaAnterior_Quando_CompraNoCartao_Deve_SerFalso(TransacoesDeTeste transacoes, Cartao cartao, LancamentoConta lancamento)
    {
        TransacaoCartao transacao = transacoes.DoCartao(cartao, lancamento.SemCategoria().ComoSaida());

        transacao.EhPagamentoFaturaAnterior.Should().BeFalse();
    }

    [Theory, TransacaoAutoData]
    public void EhCompraAVista_Quando_PagamentoFatura_Deve_SerFalso(TransacoesDeTeste transacoes, Cartao cartao, LancamentoCartao lancamento)
    {
        TransacaoCartao transacao = transacoes.DoCartao(cartao, lancamento.SemCategoria().ComoPagamentoFaturaAnterior());

        transacao.EhCompraAVista.Should().BeFalse();
    }

    [Theory, TransacaoAutoData]
    public void CompoeTotalFatura_Quando_CompraNoCartaoNoMesDaFatura_Deve_SerVerdadeiro(TransacoesDeTeste transacoes, Cartao cartao, LancamentoConta lancamento)
    {
        TransacaoCartao transacao = transacoes.DoCartao(cartao, lancamento.SemCategoria().ComoSaida());

        transacao.CompoeTotalFatura(FaturaTransacao(cartao, transacao)).Should().BeTrue();
    }

    [Theory, TransacaoAutoData]
    public void CompoeTotalFatura_Quando_EstornoNoCartao_Deve_SerVerdadeiro(TransacoesDeTeste transacoes, Cartao cartao, LancamentoConta lancamento)
    {
        TransacaoCartao transacao = transacoes.DoCartao(cartao, lancamento.SemCategoria().ComoEntrada());

        transacao.CompoeTotalFatura(FaturaTransacao(cartao, transacao)).Should().BeTrue();
    }

    [Theory, TransacaoAutoData]
    public void CompoeTotalFatura_Quando_FaturaDeOutroMes_Deve_SerFalso(TransacoesDeTeste transacoes, Cartao cartao, LancamentoConta lancamento)
    {
        TransacaoCartao transacao = transacoes.DoCartao(cartao, lancamento.SemCategoria().ComoSaida());
        FaturaCartao faturaMesSeguinte = FaturaCartao.DoCartao(cartao.Id, [cartao], transacao.MesVencimentoFatura.Posterior());

        transacao.CompoeTotalFatura(faturaMesSeguinte).Should().BeFalse();
    }

    [Theory, TransacaoAutoData]
    public void CompoeTotalFatura_Quando_FaturaDeOutroCartao_Deve_SerFalso(TransacoesDeTeste transacoes, Cartao cartao, LancamentoConta lancamento)
    {
        TransacaoCartao transacao = transacoes.DoCartao(cartao, lancamento.SemCategoria().ComoSaida());

        transacao.CompoeTotalFatura(FaturaTransacao(cartao.EmOutroBanco(), transacao)).Should().BeFalse();
    }

    [Theory, TransacaoAutoData]
    public void CompoeTotalFatura_Quando_PagamentoFaturaAnterior_Deve_SerFalso(TransacoesDeTeste transacoes, Cartao cartao, LancamentoCartao lancamento)
    {
        TransacaoCartao transacao = transacoes.DoCartao(cartao, lancamento.SemCategoria().ComoPagamentoFaturaAnterior());

        transacao.CompoeTotalFatura(FaturaTransacao(cartao, transacao)).Should().BeFalse();
    }

    [Theory, TransacaoAutoData]
    public void EhParcelaPaga_Quando_SaidaParcelada_Deve_SerVerdadeiro(TransacoesDeTeste transacoes, Cartao cartao, LancamentoCartao lancamento)
    {
        TransacaoCartao transacao = transacoes.DoCartao(cartao, lancamento.SemCategoria().ComoSaida());

        transacao.EhParcelaPaga.Should().BeTrue();
    }

    [Theory, TransacaoAutoData]
    public void EhParcelaPaga_Quando_EntradaParcelada_Deve_SerFalso(TransacoesDeTeste transacoes, Cartao cartao, LancamentoCartao lancamento)
    {
        TransacaoCartao transacao = transacoes.DoCartao(cartao, lancamento.SemCategoria().ComoEntrada());

        transacao.EhParcelaPaga.Should().BeFalse();
    }

    [Theory, TransacaoAutoData]
    public void EhParcelaPaga_Quando_AVista_Deve_SerFalso(TransacoesDeTeste transacoes, Cartao cartao, LancamentoConta lancamento)
    {
        TransacaoCartao transacao = transacoes.DoCartao(cartao, lancamento.SemCategoria().ComoSaida());

        transacao.EhParcelaPaga.Should().BeFalse();
    }

    [Theory, TransacaoAutoData]
    public void TextoBeneficiario_Quando_Saida_Deve_SerADescricao(TransacoesDeTeste transacoes, Cartao cartao, LancamentoCartao lancamento)
    {
        TransacaoCartao transacao = transacoes.DoCartao(cartao, lancamento.SemCategoria().ComoSaida());

        transacao.TextoBeneficiario.Should().Be(lancamento.Descricao);
    }

    [Theory, TransacaoAutoData]
    public void TextoBeneficiario_Quando_Entrada_Deve_SerNulo(TransacoesDeTeste transacoes, Cartao cartao, LancamentoCartao lancamento)
    {
        TransacaoCartao transacao = transacoes.DoCartao(cartao, lancamento.SemCategoria().ComoEntrada());

        transacao.TextoBeneficiario.Should().BeNull();
    }

    /// <summary>A fatura do <paramref name="cartao"/> no mês em que vence a fatura que trouxe a transação.</summary>
    private static FaturaCartao FaturaTransacao(Cartao cartao, TransacaoCartao transacao) =>
        FaturaCartao.DoCartao(cartao.Id, [cartao], transacao.MesVencimentoFatura);

    /// <summary>Tudo sai do mesmo usuário.</summary>
    private sealed class TransacaoAutoDataAttribute : DomainAutoDataAttribute
    {
        public TransacaoAutoDataAttribute()
            : base(fixture => fixture.Freeze<IdUsuario>())
        {
        }
    }
}
