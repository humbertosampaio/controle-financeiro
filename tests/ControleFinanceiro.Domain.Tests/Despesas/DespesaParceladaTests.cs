using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;
using SaidaDeTeste = ControleFinanceiro.Domain.Tests.Services.VinculoTransacaoServiceTests.SaidaDeTeste;

namespace ControleFinanceiro.Domain.Tests.Despesas;

/// <summary>O <c>Criar</c> é interno: a despesa nasce pela <see cref="DespesaParceladaFactory"/>, como na aplicação.</summary>
public sealed class DespesaParceladaTests
{
    private static readonly DateOnly _dataOcorrencia = new(2026, 01, 15);

    /// <summary>
    /// O vencimento exato da fatura da primeira parcela, num dia que o cartão não pode ter (a faixa vai até o 24), para o
    /// teste distinguir a data exata do arquivo do dia de vencimento do cartão.
    /// </summary>
    private static readonly DateOnly _vencimentoPrimeiraFatura = new(2026, 02, 25);

    [Theory, DomainAutoData]
    public void Criar_Deve_PreencherADespesaNoCredito([Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        DespesaParcelada result = Parcelar(idUsuario, cartao, numero: 1, total: 2);

        result.IdUsuario.Should().Be(idUsuario);
        result.DataOcorrencia.Should().Be(_dataOcorrencia);
        result.FormaPagamento.Should().Be(FormaPagamento.Credito);
        result.IdCartao.Should().Be(cartao.Id);
        result.IdDespesaRecorrente.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_NumerarTodasAsParcelasDaCompra([Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        DespesaParcelada result = Parcelar(idUsuario, cartao, numero: 3, total: 10);

        result.Parcelas.Select(p => p.NumeroParcela).Should().Equal(Enumerable.Range(start: 1, count: 10));
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_VencerAParcelaInformadaNaDataExataDaFatura([Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        DespesaParcelada result = Parcelar(idUsuario, cartao, numero: 3, total: 10);

        result.Parcelas.Single(p => p.NumeroParcela == 3).Pagamento.DataProjetada.Should().Be(VencimentoInformado(numero: 3));
    }

    // Doze parcelas viram o ano qualquer que seja o mês da primeira.
    [Theory, DomainAutoData]
    public void Criar_Quando_ParcelasViramOAno_Deve_VencerAsOutrasEmMesesConsecutivosNoDiaDoCartao([Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        DespesaParcelada result = Parcelar(idUsuario, cartao, numero: 3, total: 12);

        MesReferencia mes = _vencimentoPrimeiraFatura;
        foreach (ParcelaDespesa parcela in result.Parcelas)
        {
            parcela.MesReferencia.Should().Be(mes);
            if (parcela.NumeroParcela != 3)
                parcela.Pagamento.DataProjetada.Should().Be(mes.NoDia(cartao.DiaVencimento));

            mes = mes.Posterior();
        }
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_MarcarComoPagasAsParcelasAnterioresAInformada([Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        DespesaParcelada result = Parcelar(idUsuario, cartao, numero: 3, total: 10);

        result.Parcelas.Take(2).Should().AllSatisfy(p =>
        {
            p.Pagamento.Status.Should().Be(StatusPagamento.Pago);
            p.Pagamento.DataEfetiva.Should().NotBeNull().And.Be(p.Pagamento.DataProjetada);
        });
        result.Parcelas.Skip(2).Should().AllSatisfy(p =>
        {
            p.Pagamento.Status.Should().Be(StatusPagamento.Pendente);
            p.Pagamento.DataEfetiva.Should().BeNull();
        });
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_DarOMesmoValorATodasAsParcelas([Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        DespesaParcelada result = Parcelar(idUsuario, cartao, numero: 3, total: 10);

        result.Parcelas.Should().OnlyContain(p => p.Pagamento.ValorProjetado == (ReaisPositivo)99.90m);
    }

    [Theory]
    [InlineDomainAutoData(1, 0)]
    [InlineDomainAutoData(2, 1)]
    public void Criar_Quando_CompraEmDuasParcelas_Deve_PagarSoAsAnteriores(int numero, int pagasEsperadas, [Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        DespesaParcelada result = Parcelar(idUsuario, cartao, numero, total: 2);

        result.Parcelas.Should().HaveCount(2);
        result.Parcelas.Count(p => p.Pagamento.EstaPago).Should().Be(pagasEsperadas);
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_GerarIdVersao7ParaADespesaEParaCadaParcela(DespesaParcelada despesa)
    {
        despesa.Id.Value.Version.Should().Be(7);
        despesa.Parcelas.Should().OnlyContain(p => p.Id.Value.Version == 7);
    }

    [Theory, DomainAutoData]
    public void ParcelaDoMes_Quando_HaParcelaNoMes_Deve_RetornarAParcela(DespesaParcelada despesa)
    {
        ParcelaDespesa ultima = despesa.Parcelas[^1];

        Maybe<ParcelaDespesa> result = despesa.ParcelaDoMes(ultima.MesReferencia);

        result.HasValue.Should().BeTrue();
        result.Value.Should().BeSameAs(ultima);
    }

    [Theory, DomainAutoData]
    public void ParcelaDoMes_Quando_NaoHaParcelaNoMes_Deve_RetornarVazio(DespesaParcelada despesa)
    {
        Maybe<ParcelaDespesa> result = despesa.ParcelaDoMes(despesa.Parcelas[^1].MesReferencia.Posterior());

        result.HasNoValue.Should().BeTrue();
    }

    [Theory, DomainAutoData]
    public void PagamentoDoMes_Quando_HaParcelaNoMes_Deve_RetornarOPagamentoDaParcela(DespesaParcelada despesa)
    {
        ParcelaDespesa ultima = despesa.Parcelas[^1];

        Maybe<Pagamento> result = despesa.PagamentoDoMes(ultima.MesReferencia);

        result.Should().HaveValue(ultima.Pagamento);
    }

    [Theory, DomainAutoData]
    public void PagamentoDoMes_Quando_NaoHaParcelaNoMes_Deve_RetornarVazio(DespesaParcelada despesa)
    {
        Maybe<Pagamento> result = despesa.PagamentoDoMes(despesa.Parcelas[^1].MesReferencia.Posterior());

        result.Should().HaveNoValue();
    }

    [Theory, DomainAutoData]
    public void ValorVigenteDoMes_Quando_ParcelaPendente_Deve_RetornarOValorProjetado(DespesaParcelada despesa)
    {
        ParcelaDespesa ultima = despesa.Parcelas[^1];

        Maybe<ReaisPositivo> result = despesa.ValorVigenteDoMes(ultima.MesReferencia);

        result.Should().HaveValue(ultima.ValorProjetado);
    }

    [Theory, DomainAutoData]
    public void ValorVigenteDoMes_Quando_NaoHaParcelaNoMes_Deve_RetornarVazio(DespesaParcelada despesa)
    {
        Maybe<ReaisPositivo> result = despesa.ValorVigenteDoMes(despesa.Parcelas[^1].MesReferencia.Posterior());

        result.Should().HaveNoValue();
    }

    [Theory, DomainAutoData]
    public void EhDaCompra_Quando_MesmaChave_Deve_RetornarTrueSemContarCaixaEAcentos([Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        DespesaParcelada despesa = Parcelar(idUsuario, cartao, numero: 3, total: 10);

        ChaveCompraParcelada chave = new(cartao.Id, _dataOcorrencia, (TextoNormalizado)(TextoObrigatorio)"Lója de  Exemplo", totalParcelas: 10);

        despesa.EhDaCompra(chave).Should().BeTrue();
    }

    [Theory, DomainAutoData]
    public void EhDaCompra_Quando_OutroCartao_Deve_RetornarFalse([Frozen] IdUsuario idUsuario, Cartao cartao, IdCartao outroCartao)
    {
        DespesaParcelada despesa = Parcelar(idUsuario, cartao, numero: 3, total: 10);

        ChaveCompraParcelada chave = new(outroCartao, _dataOcorrencia, (TextoNormalizado)despesa.Descricao, totalParcelas: 10);

        despesa.EhDaCompra(chave).Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void EhDaCompra_Quando_OutraData_Deve_RetornarFalse([Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        DespesaParcelada despesa = Parcelar(idUsuario, cartao, numero: 3, total: 10);

        ChaveCompraParcelada chave = new(cartao.Id, _dataOcorrencia.AddDays(1), (TextoNormalizado)despesa.Descricao, totalParcelas: 10);

        despesa.EhDaCompra(chave).Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void EhDaCompra_Quando_OutraDescricao_Deve_RetornarFalse([Frozen] IdUsuario idUsuario, Cartao cartao, TextoObrigatorio outraDescricao)
    {
        DespesaParcelada despesa = Parcelar(idUsuario, cartao, numero: 3, total: 10);

        ChaveCompraParcelada chave = new(cartao.Id, _dataOcorrencia, (TextoNormalizado)outraDescricao, totalParcelas: 10);

        despesa.EhDaCompra(chave).Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void EhDaCompra_Quando_OutroTotal_Deve_RetornarFalse([Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        DespesaParcelada despesa = Parcelar(idUsuario, cartao, numero: 3, total: 10);

        ChaveCompraParcelada chave = new(cartao.Id, _dataOcorrencia, (TextoNormalizado)despesa.Descricao, totalParcelas: 12);

        despesa.EhDaCompra(chave).Should().BeFalse();
    }

    [Theory, PagaPelaFaturaAutoData]
    public void EstaPagaPelaFatura_Quando_ParcelaDoMesPagaPelaSaida_Deve_SerVerdadeiro(
        [Frozen] Cartao cartao,
        DespesaParcelada sut,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste pagamento)
    {
        CobrarEPagarPrimeira(sut, cartao, transacoes, compra, pagamento);

        sut.EstaPagaPelaFatura(FaturaCartao.DoCartao(cartao.Id, [cartao], sut.Parcelas[0].MesReferencia)).Should().BeTrue();
    }

    [Theory, PagaPelaFaturaAutoData]
    public void EstaPagaPelaFatura_Quando_ParcelaDoMesPendente_Deve_SerFalso(
        [Frozen] Cartao cartao,
        DespesaParcelada sut,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste pagamento)
    {
        CobrarEPagarPrimeira(sut, cartao, transacoes, compra, pagamento);

        sut.EstaPagaPelaFatura(FaturaCartao.DoCartao(cartao.Id, [cartao], sut.Parcelas[1].MesReferencia)).Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void EstaPagaPelaFatura_Quando_ParcelaNascidaPaga_Deve_SerFalso([Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        DespesaParcelada sut = Parcelar(idUsuario, cartao, numero: 3, total: 10);

        sut.EstaPagaPelaFatura(FaturaCartao.DoCartao(cartao.Id, [cartao], sut.Parcelas[0].MesReferencia)).Should().BeFalse();
    }

    [Theory, PagaPelaFaturaAutoData]
    public void EstaPagaPelaFatura_Quando_FaturaDeOutroCartao_Deve_SerFalso(
        [Frozen] Cartao cartao,
        DespesaParcelada sut,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste pagamento)
    {
        CobrarEPagarPrimeira(sut, cartao, transacoes, compra, pagamento);
        Cartao cartaoOutroBanco = cartao.EmOutroBanco();

        sut.EstaPagaPelaFatura(FaturaCartao.DoCartao(cartaoOutroBanco.Id, [cartaoOutroBanco], sut.Parcelas[0].MesReferencia)).Should().BeFalse();
    }

    private static void CobrarEPagarPrimeira(DespesaParcelada despesa, Cartao cartao, TransacoesDeTeste transacoes, SaidaDeTeste compra, SaidaDeTeste pagamento)
    {
        Transacao parcela = transacoes.DoCartao(cartao, compra.DaCompra(despesa, numero: 1));
        VinculoTransacaoService.Vincular(parcela, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None, cartoesDoUsuario: [cartao]).Should().Succeed();
        PagamentoFaturaService
            .Pagar(transacoes.DaConta(pagamento.AVista()), cartao, despesa.Parcelas[0].MesReferencia, [cartao], [despesa], despesasPagasPelaSaida: [])
            .Should().Succeed();
    }

    /// <summary>O vencimento exato da fatura que traz a parcela de número <paramref name="numero"/>.</summary>
    private static DateOnly VencimentoInformado(int numero) => _vencimentoPrimeiraFatura.AddMonths(numero - 1);

    private static DespesaParcelada Parcelar(IdUsuario idUsuario, Cartao cartao, int numero, int total)
    {
        Lancamento compra = Lancamento.DoCartao(
            _dataOcorrencia,
            hora: null,
            valor: -99.90m,
            (TextoObrigatorio)"LOJA DE EXEMPLO",
            contraparte: null,
            (Parcela)(numero, total),
            categoriaBanco: null,
            IdentidadeLancamento.Unica("1").Value);

        return DespesaParceladaFactory
            .CriarDaFatura(
                idUsuario,
                BeneficiariosResolvidos.Dos(idUsuario, [compra]),
                categoriasDoUsuario: [],
                cartao,
                cartao.FaturaCom((VencimentoFatura)VencimentoInformado(numero), compra),
                parceladasDoUsuario: [])
            .Single();
    }

    /// <summary>
    /// Tudo sai do mesmo usuário e na mesma data, para a compra cair no mês de vencimento da primeira parcela. A parcelada
    /// nasce na primeira de 10 parcelas, com as outras pendentes depois.
    /// </summary>
    private sealed class PagaPelaFaturaAutoDataAttribute : DomainAutoDataAttribute
    {
        public PagaPelaFaturaAutoDataAttribute()
            : base(fixture =>
            {
                fixture.Freeze<IdUsuario>();
                fixture.Freeze<DateOnly>();
                fixture.Register(() => (Parcela)(1, 10));
            })
        {
        }
    }
}
