using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;
using SaidaDeTeste = ControleFinanceiro.Domain.Tests.Services.VinculoTransacaoServiceTests.SaidaDeTeste;

namespace ControleFinanceiro.Domain.Tests.Services;

public sealed class PagamentoFaturaServiceTests
{
    [Theory, PagamentoFaturaAutoData]
    public void Pagar_Quando_AdiadaCobradaEPendente_Deve_PagarComADataDaSaidaEOValorCobrado(
        [Frozen] Cartao cartao,
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste pagamento)
    {
        Cobrar(despesa, cartao, transacoes.DoCartao(cartao, compra.AVista()));
        Transacao saida = transacoes.DaConta(pagamento.AVista());

        Result<PagamentoFatura, Erro> result = PagamentoFaturaService.Pagar(saida, cartao, despesa.MesReferencia, [cartao], [despesa], despesasPagasPelaSaida: []);

        result.Should().Succeed().Which.Pagas.Should().Equal(despesa);
        despesa.Pagamento.EstaPago.Should().BeTrue();
        despesa.Pagamento.IdTransacao.Should().Be(saida.Id);
        despesa.Pagamento.DataEfetiva.Should().Be(saida.Data);
        despesa.Pagamento.ValorEfetivo.Should().Be(compra.ValorPago);
    }

    [Theory, PagamentoFaturaAutoData]
    public void Pagar_Deve_DevolverOTotalCobradoDasDespesasPagas(
        [Frozen] Cartao cartao,
        DespesaAdiada despesa,
        DespesaAdiada outra,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste outraCompra,
        SaidaDeTeste pagamento)
    {
        Cobrar(despesa, cartao, transacoes.DoCartao(cartao, compra.AVista()));
        Cobrar(outra, cartao, transacoes.DoCartao(cartao, outraCompra.AVista()));

        Result<PagamentoFatura, Erro> result = PagamentoFaturaService.Pagar(transacoes.DaConta(pagamento.AVista()), cartao, despesa.MesReferencia, [cartao], [despesa, outra], despesasPagasPelaSaida: []);

        result.Should().Succeed().Which.Total.Value.Should().Be(compra.ValorPago.Value + outraCompra.ValorPago.Value);
    }

    [Theory, PagamentoFaturaAutoData]
    public void Pagar_Quando_ParcelasCobradas_Deve_SomarSoAParcelaDoMesNoTotal(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste outraCompra,
        SaidaDeTeste pagamento)
    {
        Cobrar(despesa, cartao, transacoes.DoCartao(cartao, compra.DaCompra(despesa, numero: 1)));
        Cobrar(despesa, cartao, transacoes.DoCartao(cartao, outraCompra.DaCompra(despesa, numero: 2)));

        Result<PagamentoFatura, Erro> result = PagamentoFaturaService.Pagar(transacoes.DaConta(pagamento.AVista()), cartao, despesa.Parcelas[0].MesReferencia, [cartao], [despesa], despesasPagasPelaSaida: []);

        result.Should().Succeed().Which.Total.Should().Be(compra.ValorPago);
    }

    [Theory, PagamentoFaturaAutoData]
    public void Pagar_Quando_ParcelasCobradas_Deve_PagarSoAParcelaDoMes(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste pagamento)
    {
        Cobrar(despesa, cartao, transacoes.DoCartao(cartao, compra.DaCompra(despesa, numero: 1)));
        Cobrar(despesa, cartao, transacoes.DoCartao(cartao, compra.DaCompra(despesa, numero: 2)));
        Transacao saida = transacoes.DaConta(pagamento.AVista());
        Pagamento segunda = despesa.Parcelas[1].Pagamento;

        Result<PagamentoFatura, Erro> result = PagamentoFaturaService.Pagar(saida, cartao, despesa.Parcelas[0].MesReferencia, [cartao], [despesa], despesasPagasPelaSaida: []);

        result.Should().Succeed().Which.Pagas.Should().Equal(despesa);
        despesa.Parcelas[0].Pagamento.IdTransacao.Should().Be(saida.Id);
        despesa.Parcelas[0].Pagamento.DataEfetiva.Should().Be(saida.Data);
        despesa.Parcelas[0].Pagamento.ValorEfetivo.Should().Be(compra.ValorPago);
        despesa.Parcelas[1].Pagamento.Should().Be(segunda);
    }

    [Theory, PagamentoFaturaAutoData]
    public void Pagar_Quando_DoisFinaisDaMesmaFatura_Deve_PagarAsCobrancasDosDois(
        [Frozen] Cartao cartao,
        DespesaAdiada doCartao,
        SufixoCartao outroSufixo,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste pagamento,
        Fixture fixture)
    {
        Cartao outroFinal = cartao.OutroFinal(outroSufixo);
        DespesaAdiada doOutroFinal = AdiadaDo(outroFinal, fixture);
        Cobrar(doCartao, cartao, transacoes.DoCartao(cartao, compra.AVista()));
        Cobrar(doOutroFinal, outroFinal, transacoes.DoCartao(outroFinal, compra.AVista()));
        Transacao saida = transacoes.DaConta(pagamento.AVista());

        Result<PagamentoFatura, Erro> result = PagamentoFaturaService.Pagar(saida, cartao, doCartao.MesReferencia, [cartao, outroFinal], [doCartao, doOutroFinal], despesasPagasPelaSaida: []);

        result.Should().Succeed().Which.Pagas.Should().Equal(doCartao, doOutroFinal);
        doOutroFinal.Pagamento.IdTransacao.Should().Be(saida.Id);
    }

    [Theory, PagamentoFaturaAutoData]
    public void Pagar_Quando_CartaoDeOutraFatura_Deve_DeixarADespesaDeleComoEsta(
        [Frozen] Cartao cartao,
        DespesaAdiada doCartao,
        SufixoCartao outroSufixo,
        DiaVencimentoCartao outroVencimento,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste pagamento,
        Fixture fixture)
    {
        Cartao outroCartao = cartao.OutroFinal(outroSufixo);
        DespesaAdiada doOutroCartao = AdiadaDo(outroCartao, fixture);
        outroCartao.AlterarCicloFatura(outroVencimento, cartao.DiaFechamento);
        Cobrar(doCartao, cartao, transacoes.DoCartao(cartao, compra.AVista()));
        Cobrar(doOutroCartao, outroCartao, transacoes.DoCartao(outroCartao, compra.AVista()));
        Pagamento pendente = doOutroCartao.Pagamento;

        Result<PagamentoFatura, Erro> result = PagamentoFaturaService.Pagar(transacoes.DaConta(pagamento.AVista()), cartao, doCartao.MesReferencia, [cartao, outroCartao], [doCartao, doOutroCartao], despesasPagasPelaSaida: []);

        result.Should().Succeed().Which.Pagas.Should().Equal(doCartao);
        doOutroCartao.Pagamento.Should().Be(pendente);
    }

    [Theory, PagamentoFaturaAutoData]
    public void Pagar_Quando_SimplesNoMes_Deve_DeixarASimplesComoEsta(
        [Frozen] Cartao cartao,
        DespesaAdiada adiada,
        DespesaSimples simples,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste pagamento)
    {
        Cobrar(adiada, cartao, transacoes.DoCartao(cartao, compra.AVista()));
        Pagamento pendente = simples.Pagamento;

        Result<PagamentoFatura, Erro> result = PagamentoFaturaService.Pagar(transacoes.DaConta(pagamento.AVista()), cartao, adiada.MesReferencia, [cartao], [simples, adiada], despesasPagasPelaSaida: []);

        result.Should().Succeed().Which.Pagas.Should().Equal(adiada);
        simples.Pagamento.Should().Be(pendente);
    }

    [Theory, PagamentoFaturaAutoData]
    public void Pagar_Quando_OutroMes_Deve_RetornarSemCobrancaPendenteEDeixarADespesaComoEsta(
        [Frozen] Cartao cartao,
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste pagamento)
    {
        Cobrar(despesa, cartao, transacoes.DoCartao(cartao, compra.AVista()));
        Pagamento pendente = despesa.Pagamento;

        Result<PagamentoFatura, Erro> result = PagamentoFaturaService.Pagar(transacoes.DaConta(pagamento.AVista()), cartao, despesa.MesReferencia.Posterior(), [cartao], [despesa], despesasPagasPelaSaida: []);

        result.Should().Fail().Which.Should().Be(ErroFaturaSemCobrancaPendente.Instancia);
        despesa.Pagamento.Should().Be(pendente);
    }

    [Theory, PagamentoFaturaAutoData]
    public void Pagar_Quando_PendenteSemCobranca_Deve_RetornarSemCobrancaPendenteEDeixarADespesaComoEsta(
        [Frozen] Cartao cartao,
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste pagamento)
    {
        Pagamento pendente = despesa.Pagamento;

        Result<PagamentoFatura, Erro> result = PagamentoFaturaService.Pagar(transacoes.DaConta(pagamento.AVista()), cartao, despesa.MesReferencia, [cartao], [despesa], despesasPagasPelaSaida: []);

        result.Should().Fail().Which.Should().Be(ErroFaturaSemCobrancaPendente.Instancia);
        despesa.Pagamento.Should().Be(pendente);
    }

    [Theory, PagamentoFaturaAutoData]
    public void Pagar_Quando_DespesaJaPagaPorOutraSaida_Deve_RetornarSemCobrancaPendenteEManterOPagamento(
        [Frozen] Cartao cartao,
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste pagamento,
        SaidaDeTeste outroPagamento)
    {
        Cobrar(despesa, cartao, transacoes.DoCartao(cartao, compra.AVista()));
        PagamentoFaturaService.Pagar(transacoes.DaConta(pagamento.AVista()), cartao, despesa.MesReferencia, [cartao], [despesa], despesasPagasPelaSaida: []).Should().Succeed();
        Pagamento pago = despesa.Pagamento;

        Result<PagamentoFatura, Erro> result = PagamentoFaturaService.Pagar(transacoes.DaConta(outroPagamento.AVista()), cartao, despesa.MesReferencia, [cartao], [despesa], despesasPagasPelaSaida: []);

        result.Should().Fail().Which.Should().Be(ErroFaturaSemCobrancaPendente.Instancia);
        despesa.Pagamento.Should().Be(pago);
    }

    [Theory, PagamentoFaturaAutoData]
    public void Pagar_Quando_ParcelaNasceuPagaECobrada_Deve_RetornarSemCobrancaPendenteEManterOPagamento(
        [Frozen] Cartao cartao,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste pagamento,
        Fixture fixture)
    {
        fixture.Inject((Parcela)(3, 10));
        DespesaParcelada despesa = fixture.Create<DespesaParcelada>();
        Cobrar(despesa, cartao, transacoes.DoCartao(cartao, compra.DaCompra(despesa, numero: 1)));
        Pagamento paga = despesa.Parcelas[0].Pagamento;

        Result<PagamentoFatura, Erro> result = PagamentoFaturaService.Pagar(transacoes.DaConta(pagamento.AVista()), cartao, despesa.Parcelas[0].MesReferencia, [cartao], [despesa], despesasPagasPelaSaida: []);

        result.Should().Fail().Which.Should().Be(ErroFaturaSemCobrancaPendente.Instancia);
        despesa.Parcelas[0].Pagamento.Should().Be(paga);
    }

    [Theory, PagamentoFaturaAutoData]
    public void Pagar_Quando_MesmaSaidaDepoisDeNovaCobranca_Deve_PagarSoANova(
        [Frozen] Cartao cartao,
        DespesaAdiada primeira,
        DespesaAdiada nova,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste pagamento)
    {
        Transacao saida = transacoes.DaConta(pagamento.AVista());
        Cobrar(primeira, cartao, transacoes.DoCartao(cartao, compra.AVista()));
        PagamentoFaturaService.Pagar(saida, cartao, primeira.MesReferencia, [cartao], [primeira, nova], despesasPagasPelaSaida: []).Should().Succeed();
        Cobrar(nova, cartao, transacoes.DoCartao(cartao, compra.AVista()));

        Result<PagamentoFatura, Erro> result = PagamentoFaturaService.Pagar(saida, cartao, primeira.MesReferencia, [cartao], [primeira, nova], [primeira]);

        result.Should().Succeed().Which.Pagas.Should().Equal(nova);
        nova.Pagamento.IdTransacao.Should().Be(saida.Id);
    }

    [Theory, PagamentoFaturaAutoData]
    public void Pagar_Quando_SaidaJaPagouOutroMes_Deve_RetornarPagaOutraFatura(
        [Frozen] Cartao cartao,
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste pagamento)
    {
        Transacao saida = transacoes.DaConta(pagamento.AVista());
        Cobrar(despesa, cartao, transacoes.DoCartao(cartao, compra.AVista()));
        PagamentoFaturaService.Pagar(saida, cartao, despesa.MesReferencia, [cartao], [despesa], despesasPagasPelaSaida: []).Should().Succeed();

        Result<PagamentoFatura, Erro> result = PagamentoFaturaService.Pagar(saida, cartao, despesa.MesReferencia.Posterior(), [cartao], despesasDoMes: [], [despesa]);

        result.Should().Fail().Which.Should().Be(ErroTransacaoPagaOutraFatura.Instancia);
    }

    [Theory, PagamentoFaturaAutoData]
    public void Pagar_Quando_SaidaJaPagouOutraFatura_Deve_RetornarPagaOutraFaturaSemPagar(
        [Frozen] Cartao cartao,
        DespesaAdiada doCartao,
        SufixoCartao outroSufixo,
        DiaVencimentoCartao outroVencimento,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste pagamento,
        Fixture fixture)
    {
        Cartao outroCartao = cartao.OutroFinal(outroSufixo);
        DespesaAdiada doOutroCartao = AdiadaDo(outroCartao, fixture);
        outroCartao.AlterarCicloFatura(outroVencimento, cartao.DiaFechamento);
        Transacao saida = transacoes.DaConta(pagamento.AVista());
        Cobrar(doOutroCartao, outroCartao, transacoes.DoCartao(outroCartao, compra.AVista()));
        PagamentoFaturaService.Pagar(saida, outroCartao, doOutroCartao.MesReferencia, [cartao, outroCartao], [doOutroCartao], despesasPagasPelaSaida: []).Should().Succeed();
        Cobrar(doCartao, cartao, transacoes.DoCartao(cartao, compra.AVista()));
        Pagamento pendente = doCartao.Pagamento;

        Result<PagamentoFatura, Erro> result = PagamentoFaturaService.Pagar(saida, cartao, doCartao.MesReferencia, [cartao, outroCartao], [doCartao], [doOutroCartao]);

        result.Should().Fail().Which.Should().Be(ErroTransacaoPagaOutraFatura.Instancia);
        doCartao.Pagamento.Should().Be(pendente);
    }

    [Theory, PagamentoFaturaAutoData]
    public void Pagar_Quando_SaidaJaPagaUmaSimples_Deve_RetornarJaVinculadaComASimples(
        [Frozen] Cartao cartao,
        DespesaAdiada adiada,
        DespesaSimples simples,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste pagamento)
    {
        Transacao saida = transacoes.DaConta(pagamento.AVista());
        VinculoTransacaoService.Vincular(saida, simples, despesaQueUsaATransacao: Maybe<Despesa>.None, cartoesDoUsuario: []).Should().Succeed();
        Cobrar(adiada, cartao, transacoes.DoCartao(cartao, compra.AVista()));

        Result<PagamentoFatura, Erro> result = PagamentoFaturaService.Pagar(saida, cartao, adiada.MesReferencia, [cartao], [adiada], [simples]);

        result.Should().Fail()
            .Which.Should().BeOfType<ErroTransacaoJaVinculada>()
            .Which.IdDespesa.Should().Be(simples.Id);
        adiada.Pagamento.EstaPago.Should().BeFalse();
    }

    [Theory, PagamentoFaturaAutoData]
    public void Pagar_Quando_SaidaDoCartao_Deve_RetornarOrigemIncompativel(
        [Frozen] Cartao cartao,
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste pagamento)
    {
        Cobrar(despesa, cartao, transacoes.DoCartao(cartao, compra.AVista()));

        Result<PagamentoFatura, Erro> result = PagamentoFaturaService.Pagar(transacoes.DoCartao(cartao, pagamento.AVista()), cartao, despesa.MesReferencia, [cartao], [despesa], despesasPagasPelaSaida: []);

        result.Should().Fail().Which.Should().Be(ErroOrigemIncompativelComDespesa.Instancia);
        despesa.Pagamento.EstaPago.Should().BeFalse();
    }

    [Theory]
    [InlinePagamentoFaturaAutoData(50.00)]
    [InlinePagamentoFaturaAutoData(0.00)]
    public void Pagar_Quando_TransacaoNaoEhSaida_Deve_RetornarNaoEhSaida(
        decimal valor,
        [Frozen] Cartao cartao,
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste pagamento)
    {
        Cobrar(despesa, cartao, transacoes.DoCartao(cartao, compra.AVista()));

        Result<PagamentoFatura, Erro> result = PagamentoFaturaService.Pagar(transacoes.DaConta(pagamento.ComValor(valor)), cartao, despesa.MesReferencia, [cartao], [despesa], despesasPagasPelaSaida: []);

        result.Should().Fail().Which.Should().Be(ErroTransacaoNaoEhSaida.Instancia);
        despesa.Pagamento.EstaPago.Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void Pagar_Quando_CartaoDeOutroUsuario_Deve_LancarExcecao(
        Cartao deOutroUsuario,
        [Frozen] IdUsuario idUsuario,
        TransacoesDeTeste transacoes,
        SaidaDeTeste pagamento,
        MesReferencia mesVencimento)
    {
        Transacao saida = transacoes.DaConta(pagamento.AVista());
        saida.IdUsuario.Should().Be(idUsuario);

        Action act = () => PagamentoFaturaService.Pagar(saida, deOutroUsuario, mesVencimento, cartoesDoUsuario: [], despesasDoMes: [], despesasPagasPelaSaida: []);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("cartao");
    }

    [Theory, DomainAutoData]
    public void Pagar_Quando_DespesaDoMesDeOutroUsuario_Deve_LancarExcecao(
        DespesaAdiada deOutroUsuario,
        [Frozen] IdUsuario idUsuario,
        Cartao cartao,
        TransacoesDeTeste transacoes,
        SaidaDeTeste pagamento)
    {
        cartao.IdUsuario.Should().Be(idUsuario);

        Action act = () => PagamentoFaturaService.Pagar(transacoes.DaConta(pagamento.AVista()), cartao, deOutroUsuario.MesReferencia, [cartao], [deOutroUsuario], despesasPagasPelaSaida: []);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("despesasDoMes");
    }

    [Theory, DomainAutoData]
    public void Pagar_Quando_CartaoDaListaDeOutroUsuario_Deve_LancarExcecao(
        Cartao deOutroUsuario,
        [Frozen] IdUsuario idUsuario,
        Cartao cartao,
        TransacoesDeTeste transacoes,
        SaidaDeTeste pagamento,
        MesReferencia mesVencimento)
    {
        cartao.IdUsuario.Should().Be(idUsuario);

        Action act = () => PagamentoFaturaService.Pagar(transacoes.DaConta(pagamento.AVista()), cartao, mesVencimento, [cartao, deOutroUsuario], despesasDoMes: [], despesasPagasPelaSaida: []);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("cartoesDoUsuario");
    }

    [Theory, DomainAutoData]
    public void Pagar_Quando_DespesaPagaPelaSaidaDeOutroUsuario_Deve_LancarExcecao(
        DespesaAdiada deOutroUsuario,
        [Frozen] IdUsuario idUsuario,
        Cartao cartao,
        TransacoesDeTeste transacoes,
        SaidaDeTeste pagamento)
    {
        cartao.IdUsuario.Should().Be(idUsuario);

        Action act = () => PagamentoFaturaService.Pagar(transacoes.DaConta(pagamento.AVista()), cartao, deOutroUsuario.MesReferencia, [cartao], despesasDoMes: [], [deOutroUsuario]);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("despesasPagasPelaSaida");
    }

    [Theory, PagamentoFaturaAutoData]
    public void Desfazer_Quando_AdiadaPagaPelaSaida_Deve_VoltarAPendenteComACobranca(
        [Frozen] Cartao cartao,
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste pagamento)
    {
        Pagamento pendente = despesa.Pagamento;
        Cobrar(despesa, cartao, transacoes.DoCartao(cartao, compra.AVista()));
        Cobranca? cobranca = despesa.Cobranca;
        Transacao saida = transacoes.DaConta(pagamento.AVista());
        PagamentoFaturaService.Pagar(saida, cartao, despesa.MesReferencia, [cartao], [despesa], despesasPagasPelaSaida: []).Should().Succeed();

        UnitResult<Erro> result = PagamentoFaturaService.Desfazer(saida, [despesa]);

        result.Should().Succeed();
        despesa.Pagamento.Should().Be(pendente);
        despesa.Cobranca.Should().Be(cobranca);
    }

    [Theory, PagamentoFaturaAutoData]
    public void Desfazer_Quando_ParcelasPagasPorSaidasDiferentes_Deve_VoltarSoADaSaida(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste pagamento,
        SaidaDeTeste outroPagamento)
    {
        Pagamento pendente = despesa.Parcelas[0].Pagamento;
        Cobrar(despesa, cartao, transacoes.DoCartao(cartao, compra.DaCompra(despesa, numero: 1)));
        Cobrar(despesa, cartao, transacoes.DoCartao(cartao, compra.DaCompra(despesa, numero: 2)));
        Cobranca? cobranca = despesa.Parcelas[0].Cobranca;
        Transacao saida = transacoes.DaConta(pagamento.AVista());
        PagamentoFaturaService.Pagar(saida, cartao, despesa.Parcelas[0].MesReferencia, [cartao], [despesa], despesasPagasPelaSaida: []).Should().Succeed();
        PagamentoFaturaService.Pagar(transacoes.DaConta(outroPagamento.AVista()), cartao, despesa.Parcelas[1].MesReferencia, [cartao], [despesa], despesasPagasPelaSaida: []).Should().Succeed();
        Pagamento segunda = despesa.Parcelas[1].Pagamento;

        UnitResult<Erro> result = PagamentoFaturaService.Desfazer(saida, [despesa]);

        result.Should().Succeed();
        despesa.Parcelas[0].Pagamento.Should().Be(pendente);
        despesa.Parcelas[0].Cobranca.Should().Be(cobranca);
        despesa.Parcelas[1].Pagamento.Should().Be(segunda);
    }

    [Theory, PagamentoFaturaAutoData]
    public void Desfazer_Quando_ParcelaNasceuPaga_Deve_DeixarAParcelaPaga(
        [Frozen] Cartao cartao,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste pagamento,
        Fixture fixture)
    {
        fixture.Inject((Parcela)(2, 10));
        DespesaParcelada despesa = fixture.Create<DespesaParcelada>();
        Cobrar(despesa, cartao, transacoes.DoCartao(cartao, compra.DaCompra(despesa, numero: 2)));
        Transacao saida = transacoes.DaConta(pagamento.AVista());
        PagamentoFaturaService.Pagar(saida, cartao, despesa.Parcelas[1].MesReferencia, [cartao], [despesa], despesasPagasPelaSaida: []).Should().Succeed();
        Pagamento paga = despesa.Parcelas[0].Pagamento;

        UnitResult<Erro> result = PagamentoFaturaService.Desfazer(saida, [despesa]);

        result.Should().Succeed();
        despesa.Parcelas[0].Pagamento.Should().Be(paga);
        despesa.Parcelas[1].Pagamento.EstaPago.Should().BeFalse();
    }

    [Theory, PagamentoFaturaAutoData]
    public void Desfazer_Quando_SimplesPagaPelaMesmaSaida_Deve_DeixarASimplesPaga(
        [Frozen] Cartao cartao,
        DespesaAdiada adiada,
        DespesaSimples simples,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste pagamento)
    {
        Cobrar(adiada, cartao, transacoes.DoCartao(cartao, compra.AVista()));
        Transacao saida = transacoes.DaConta(pagamento.AVista());
        PagamentoFaturaService.Pagar(saida, cartao, adiada.MesReferencia, [cartao], [adiada], despesasPagasPelaSaida: []).Should().Succeed();
        VinculoTransacaoService.Vincular(saida, simples, despesaQueUsaATransacao: Maybe<Despesa>.None, cartoesDoUsuario: []).Should().Succeed();
        Pagamento paga = simples.Pagamento;

        UnitResult<Erro> result = PagamentoFaturaService.Desfazer(saida, [simples, adiada]);

        result.Should().Succeed();
        adiada.Pagamento.EstaPago.Should().BeFalse();
        simples.Pagamento.Should().Be(paga);
    }

    [Theory, PagamentoFaturaAutoData]
    public void Desfazer_Quando_SaidaSoPagouSimples_Deve_RetornarNaoPagouADespesaEDeixarASimplesPaga(
        DespesaSimples simples,
        TransacoesDeTeste transacoes,
        SaidaDeTeste pagamento)
    {
        Transacao saida = transacoes.DaConta(pagamento.AVista());
        VinculoTransacaoService.Vincular(saida, simples, despesaQueUsaATransacao: Maybe<Despesa>.None, cartoesDoUsuario: []).Should().Succeed();
        Pagamento paga = simples.Pagamento;

        UnitResult<Erro> result = PagamentoFaturaService.Desfazer(saida, [simples]);

        result.Should().Fail().Which.Should().Be(ErroTransacaoNaoPagouADespesa.Instancia);
        simples.Pagamento.Should().Be(paga);
    }

    [Theory, PagamentoFaturaAutoData]
    public void Desfazer_Quando_DespesaDoCartaoNaoPagaPelaSaida_Deve_RetornarNaoPagouADespesa(
        [Frozen] Cartao cartao,
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste pagamento)
    {
        Cobrar(despesa, cartao, transacoes.DoCartao(cartao, compra.AVista()));
        Pagamento pendente = despesa.Pagamento;

        UnitResult<Erro> result = PagamentoFaturaService.Desfazer(transacoes.DaConta(pagamento.AVista()), [despesa]);

        result.Should().Fail().Which.Should().Be(ErroTransacaoNaoPagouADespesa.Instancia);
        despesa.Pagamento.Should().Be(pendente);
    }

    [Theory, PagamentoFaturaAutoData]
    public void Desfazer_Quando_ParceladaSemParcelaPagaPelaSaida_Deve_RetornarNaoPagouADespesa(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste pagamento)
    {
        Cobrar(despesa, cartao, transacoes.DoCartao(cartao, compra.DaCompra(despesa, numero: 1)));
        Pagamento pendente = despesa.Parcelas[0].Pagamento;

        UnitResult<Erro> result = PagamentoFaturaService.Desfazer(transacoes.DaConta(pagamento.AVista()), [despesa]);

        result.Should().Fail().Which.Should().Be(ErroTransacaoNaoPagouADespesa.Instancia);
        despesa.Parcelas[0].Pagamento.Should().Be(pendente);
    }

    [Theory, DomainAutoData]
    public void Desfazer_Quando_DespesaDeOutroUsuario_Deve_LancarExcecao(
        DespesaAdiada deOutroUsuario,
        [Frozen] IdUsuario idUsuario,
        TransacoesDeTeste transacoes,
        SaidaDeTeste pagamento)
    {
        Transacao saida = transacoes.DaConta(pagamento.AVista());
        saida.IdUsuario.Should().Be(idUsuario);

        Action act = () => PagamentoFaturaService.Desfazer(saida, [deOutroUsuario]);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("despesasPagasPelaSaida");
    }

    private static void Cobrar(Despesa despesa, Cartao cartao, Transacao compra)
    {
        VinculoTransacaoService.Vincular(compra, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None, cartoesDoUsuario: [cartao]).Should().Succeed();
    }

    /// <summary>
    /// Uma despesa adiada do cartão montado no teste, na mesma data das demais. Trocar o cartão depois, com o
    /// <see cref="Cartao.AlterarCicloFatura"/>, não muda a despesa já gerada.
    /// </summary>
    private static DespesaAdiada AdiadaDo(Cartao cartao, Fixture fixture)
    {
        fixture.Inject(cartao);
        return fixture.Create<DespesaAdiada>();
    }

    /// <summary>
    /// Tudo sai do mesmo usuário e na mesma data, para as compras dos finais da mesma fatura caírem no mesmo mês de
    /// vencimento. A parcelada nasce na primeira de 10 parcelas, que vence nesse mês, com as outras pendentes depois.
    /// </summary>
    private sealed class PagamentoFaturaAutoDataAttribute : DomainAutoDataAttribute
    {
        public PagamentoFaturaAutoDataAttribute()
            : base(fixture =>
            {
                fixture.Freeze<IdUsuario>();
                fixture.Freeze<DateOnly>();
                fixture.Register(() => (Parcela)(1, 10));
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
