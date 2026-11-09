using AutoFixture.Xunit2;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using ControleFinanceiro.Tests.Shared.Respostas;
using CSharpFunctionalExtensions;
using System.Text.Json.Nodes;

namespace ControleFinanceiro.SaidaModelo.Tests.Tipos;

public sealed class PagamentoFaturaParaModeloTests
{
    [Theory, PagamentoFaturaAutoData]
    public void Serializar_Deve_TerSomenteOsCamposPermitidos(FaturaPagaDeTeste fatura)
    {
        JsonObject json = fatura.ParaModelo().Serializar();

        json.Select(campo => campo.Key).Should().BeEquivalentTo("transacao", "despesas", "total", "resumo");
    }

    [Theory, PagamentoFaturaAutoData]
    public void Serializar_Deve_LevarASaidaEAsDespesasPagasComOTotal(FaturaPagaDeTeste fatura)
    {
        JsonObject json = fatura.ParaModelo().Serializar();

        PagamentoFaturaLido lido = new(json);
        lido.Transacao.Id.Should().Be(fatura.Saida.Id.Value);
        lido.Despesas.Select(d => d.Id).Should().Equal(fatura.Pagamento.Pagas.Select(d => d.Id.Value));
        lido.Despesas.Should().OnlyContain(d => d.Status == StatusPagamento.Pago.Name && d.Mes == fatura.Pagamento.MesVencimento.ToString());
        lido.Total.Should().Be(fatura.Pagamento.Total);
    }

    [Fact]
    public void Serializar_Quando_Falha_Deve_TerSoOErro()
    {
        JsonObject json = PagamentoFaturaParaModelo.Falha(ErroParaModelo.FaturaSemCobrancaPendente).Serializar();

        json.Select(campo => campo.Key).Should().Equal("erro");
        ErroLido.DoEnvelope(json).Mensagem.Should().Be(ErroParaModelo.FaturaSemCobrancaPendente.Mensagem);
    }

    [Theory, PagamentoFaturaAutoData]
    public void Resumo_Deve_DescreverASaidaEAFaturaSemOsIds(FaturaPagaDeTeste fatura)
    {
        string? resumo = fatura.ParaModelo().Resumo;

        resumo.Should()
            .StartWith($"A saída de {ComEspacosComuns(fatura.Saida.ValorAbsoluto.ToString())} em ")
            .And.Contain(fatura.Saida.Descricao)
            .And.Contain($"paga a fatura do {fatura.Cartao.Banco.Name} em que sai o cartão final {fatura.Cartao.Sufixo}, ")
            .And.EndWith($"com vencimento em {fatura.Pagamento.MesVencimento}: {fatura.Pagamento.Pagas.Count} despesas, no total de {ComEspacosComuns(fatura.Pagamento.Total.ToString())}.")
            .And.NotContain(fatura.Saida.Id.Value.ToString())
            .And.NotContain(fatura.Pagamento.Pagas[0].Id.Value.ToString())
            .And.NotContain(fatura.Cartao.Id.Value.ToString());
    }

    [Theory]
    [InlinePagamentoFaturaAutoData(FormaDocumento.CpfComPontuacao)]
    [InlinePagamentoFaturaAutoData(FormaDocumento.CnpjPuro)]
    [InlinePagamentoFaturaAutoData(FormaDocumento.CnpjComLetrasComPontuacao)]
    public void Resumo_Quando_SaidaComDocumento_Deve_SairSemODocumento(
        FormaDocumento forma,
        DocumentosDeTeste documentos,
        Lancamento lancamento,
        Fixture fixture)
    {
        fixture.Inject(lancamento.ComTextos((TextoObrigatorio)$"PIX {documentos.Em(forma)}", $"FULANO {documentos.Em(forma)}"));

        string? resumo = fixture.Create<FaturaPagaDeTeste>().ParaModelo().Resumo;

        resumo.Should().NotContain(documentos.Em(forma)).And.Contain($"PIX {DocumentosDeTeste.Rotulo(forma)}");
    }

    [Theory, PagamentoFaturaAutoData]
    public void Resumo_Deve_TerSoEspacosComuns(FaturaPagaDeTeste fatura)
    {
        fatura.ParaModelo().Resumo.Should().NotContain(EspacoNaoSeparavel);
    }

    [Theory, PagamentoFaturaAutoData]
    public void ConfereResumo_Quando_ResumoIgual_Deve_Conferir(FaturaPagaDeTeste fatura)
    {
        PagamentoFaturaParaModelo pagamento = fatura.ParaModelo();

        pagamento.ConfereResumo(pagamento.Resumo!).Should().BeTrue();
    }

    [Theory, PagamentoFaturaAutoData]
    public void ConfereResumo_Quando_ResumoAlterado_Deve_NaoConferir(FaturaPagaDeTeste fatura)
    {
        PagamentoFaturaParaModelo pagamento = fatura.ParaModelo();

        pagamento.ConfereResumo(pagamento.Resumo!.Replace("paga a fatura", "paga as faturas", StringComparison.Ordinal)).Should().BeFalse();
    }

    [Fact]
    public void ConfereResumo_Quando_Falha_Deve_NaoConferir()
    {
        PagamentoFaturaParaModelo.Falha(ErroParaModelo.FaturaSemCobrancaPendente).ConfereResumo(string.Empty).Should().BeFalse();
    }

    private const string EspacoNaoSeparavel = " ";

    private static string ComEspacosComuns(string texto) => texto.Replace(EspacoNaoSeparavel, " ", StringComparison.Ordinal);

    /// <summary>
    /// A fatura do cartão paga por uma saída da conta: cada adiada é cobrada por uma compra à vista do cartão, na data
    /// dela, e a saída feita do lançamento paga todas no mês de vencimento.
    /// </summary>
    public sealed class FaturaPagaDeTeste
    {
        public Cartao Cartao { get; }
        public Transacao Saida { get; }
        public PagamentoFatura Pagamento { get; }

        /// <param name="despesas">Adiadas do cartão no mesmo mês de vencimento.</param>
        public FaturaPagaDeTeste(Cartao cartao, DespesaAdiada[] despesas, TransacoesDeTeste transacoes, Lancamento lancamento)
        {
            foreach (DespesaAdiada despesa in despesas)
            {
                Transacao compra = transacoes.DoCartao(cartao, CompraDa(despesa, lancamento));
                VinculoTransacaoService.Vincular(compra, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None, cartoesDoUsuario: [cartao]).Should().Succeed();
            }

            Cartao = cartao;
            Saida = transacoes.DaConta(lancamento.SemCategoria().ComoSaida());
            Pagamento = PagamentoFaturaService.Pagar(Saida, cartao, despesas[0].MesReferencia, [cartao], despesas, despesasPagasPelaSaida: []).Value;
        }

        public PagamentoFaturaParaModelo ParaModelo() => PagamentoFaturaParaModelo.De(Saida, Cartao, Pagamento);

        private static Lancamento CompraDa(DespesaAdiada despesa, Lancamento lancamento)
        {
            return Lancamento.DoCartao(
                despesa.DataOcorrencia,
                hora: null,
                -(decimal.Abs(lancamento.Valor) + 1m),
                despesa.Descricao,
                contraparte: null,
                parcela: null,
                categoriaBanco: null,
                lancamento.Identidade);
        }
    }

    /// <summary>Tudo sai do mesmo usuário, do mesmo cartão e na mesma data: as adiadas caem na mesma fatura.</summary>
    private sealed class PagamentoFaturaAutoDataAttribute : DomainAutoDataAttribute
    {
        public PagamentoFaturaAutoDataAttribute()
            : base(fixture =>
            {
                fixture.Freeze<IdUsuario>();
                fixture.Freeze<DateOnly>();
                fixture.Freeze<Cartao>();
            })
        {
        }
    }

    private sealed class InlinePagamentoFaturaAutoDataAttribute : InlineAutoDataAttribute
    {
        public InlinePagamentoFaturaAutoDataAttribute(params object[] valores)
            : base(new PagamentoFaturaAutoDataAttribute(), valores)
        {
        }
    }
}
