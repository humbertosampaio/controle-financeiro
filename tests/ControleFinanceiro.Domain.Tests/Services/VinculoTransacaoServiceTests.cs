using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using ControleFinanceiro.Tests.Shared;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Services;

public sealed class VinculoTransacaoServiceTests
{
    /// <summary>A parcela em que a parcelada do fixture nasce: tem parcelas pagas antes dela e pendentes depois.</summary>
    private static readonly Parcela _parcelaDaCompra = (Parcela)(3, 10);

    [Theory, VinculoAutoData]
    public void Vincular_Quando_SimplesESaidaDeConta_Deve_PagarNaDataDaTransacao(
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DaConta(saida.AVista());
        ReaisPositivo previsto = despesa.Pagamento.ValorProjetado;
        DateOnly projetada = despesa.Pagamento.DataProjetada;

        UnitResult<Erro> result = VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Succeed();
        despesa.Pagamento.EstaPago.Should().BeTrue();
        despesa.Pagamento.IdTransacao.Should().Be(transacao.Id);
        despesa.Pagamento.DataEfetiva.Should().Be(transacao.Data);
        despesa.Pagamento.DataProjetada.Should().Be(projetada);
        despesa.Pagamento.ValorProjetado.Should().Be(previsto);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_ValorDiferenteDoProjetado_Deve_RegistrarOValorDaTransacao(
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        ReaisPositivo projetado = despesa.Pagamento.ValorProjetado;

        UnitResult<Erro> result = VinculoTransacaoService.Vincular(transacoes.DaConta(saida.AVista()), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Succeed();
        despesa.Pagamento.ValorEfetivo.Should().Be(saida.ValorPago);
        despesa.Pagamento.ValorProjetado.Should().Be(projetado);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_AdiadaECompraDoMesmoCartao_Deve_CobrarSemPagar(
        [Frozen] Cartao cartao,
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Pagamento pendente = despesa.Pagamento;
        Transacao transacao = transacoes.DoCartao(cartao, saida.AVista());

        UnitResult<Erro> result = VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Succeed();
        despesa.Cobranca.Should().Be(new Cobranca(transacao.Id, saida.ValorPago));
        despesa.Pagamento.Should().Be(pendente);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_ParcelaDaCompra_Deve_CobrarSoAParcelaDoMesmoNumeroSemPagar(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Pagamento[] pagamentos = despesa.Parcelas.Select(p => p.Pagamento).ToArray();
        Transacao transacao = transacoes.DoCartao(cartao, saida.DaCompra(despesa, numero: 3));

        UnitResult<Erro> result = VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Succeed();
        despesa.Parcelas[2].Cobranca.Should().Be(new Cobranca(transacao.Id, saida.ValorPago));
        despesa.Parcelas.Where(p => p.NumeroParcela != 3).Should().OnlyContain(p => p.Cobranca == null);
        despesa.Parcelas.Select(p => p.Pagamento).Should().Equal(pagamentos);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_ParcelaNasceuPagaSemTransacao_Deve_CobrarEContinuarPaga(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        ParcelaDespesa primeira = despesa.Parcelas[0];
        Pagamento paga = primeira.Pagamento;
        paga.EstaPago.Should().BeTrue();
        Transacao transacao = transacoes.DoCartao(cartao, saida.DaCompra(despesa, numero: 1));

        UnitResult<Erro> result = VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Succeed();
        primeira.Cobranca.Should().Be(new Cobranca(transacao.Id, saida.ValorPago));
        primeira.Pagamento.Should().Be(paga);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_TransacaoJaPagaOutraDespesa_Deve_RetornarErroComADespesa(
        DespesaSimples despesa,
        DespesaSimples outra,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DaConta(saida.AVista());

        UnitResult<Erro> result = VinculoTransacaoService.Vincular(transacao, despesa, outra);

        result.Should().Fail()
            .Which.Should().BeOfType<ErroTransacaoJaVinculada>()
            .Which.IdDespesa.Should().Be(outra.Id);
        despesa.Pagamento.EstaPago.Should().BeFalse();
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_TransacaoJaPagaEstaDespesa_Deve_RetornarErroComADespesa(
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DaConta(saida.AVista());
        VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        UnitResult<Erro> result = VinculoTransacaoService.Vincular(transacao, despesa, despesa);

        result.Should().Fail()
            .Which.Should().BeOfType<ErroTransacaoJaVinculada>()
            .Which.IdDespesa.Should().Be(despesa.Id);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_DespesaJaPagaPorOutraTransacao_Deve_RetornarDespesaJaPaga(
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida,
        SaidaDeTeste outraSaida)
    {
        Transacao primeira = transacoes.DaConta(saida.AVista());
        VinculoTransacaoService.Vincular(primeira, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        UnitResult<Erro> result = VinculoTransacaoService.Vincular(transacoes.DaConta(outraSaida.AVista()), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroDespesaJaPaga.Instancia);
        despesa.Pagamento.IdTransacao.Should().Be(primeira.Id);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_ParcelaJaCobradaPorOutraTransacao_Deve_RetornarDespesaJaCobrada(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida,
        SaidaDeTeste outraSaida)
    {
        Transacao primeira = transacoes.DoCartao(cartao, saida.DaCompra(despesa, numero: 3));
        VinculoTransacaoService.Vincular(primeira, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();
        Transacao outra = transacoes.DoCartao(cartao, outraSaida.DaCompra(despesa, numero: 3));

        UnitResult<Erro> result = VinculoTransacaoService.Vincular(outra, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroDespesaJaCobrada.Instancia);
        despesa.Parcelas[2].Cobranca!.IdTransacao.Should().Be(primeira.Id);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_AdiadaJaCobradaPorOutraTransacao_Deve_RetornarDespesaJaCobrada(
        [Frozen] Cartao cartao,
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida,
        SaidaDeTeste outraSaida)
    {
        Transacao primeira = transacoes.DoCartao(cartao, saida.AVista());
        VinculoTransacaoService.Vincular(primeira, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        UnitResult<Erro> result = VinculoTransacaoService.Vincular(transacoes.DoCartao(cartao, outraSaida.AVista()), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroDespesaJaCobrada.Instancia);
        despesa.Cobranca!.IdTransacao.Should().Be(primeira.Id);
    }

    [Theory]
    [InlineVinculoAutoData(50.00)]
    [InlineVinculoAutoData(0.00)]
    public void Vincular_Quando_TransacaoNaoEhSaida_Deve_RetornarErro(
        decimal valor,
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DaConta(saida.ComValor(valor));

        UnitResult<Erro> result = VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroTransacaoNaoEhSaida.Instancia);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_SimplesComSaidaDeCartao_Deve_RetornarOrigemIncompativel(
        Cartao cartao,
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        UnitResult<Erro> result = VinculoTransacaoService.Vincular(transacoes.DoCartao(cartao, saida.AVista()), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroOrigemIncompativelComDespesa.Instancia);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_AdiadaComSaidaDeConta_Deve_RetornarOrigemIncompativel(
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        UnitResult<Erro> result = VinculoTransacaoService.Vincular(transacoes.DaConta(saida.AVista()), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroOrigemIncompativelComDespesa.Instancia);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_AdiadaComSaidaDeOutroCartao_Deve_RetornarOrigemIncompativel(
        Cartao outroCartao,
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        UnitResult<Erro> result = VinculoTransacaoService.Vincular(transacoes.DoCartao(outroCartao, saida.AVista()), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroOrigemIncompativelComDespesa.Instancia);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_ParcelaDeOutroCartao_Deve_RetornarOrigemIncompativel(
        Cartao outroCartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        UnitResult<Erro> result = VinculoTransacaoService.Vincular(transacoes.DoCartao(outroCartao, saida.DaCompra(despesa, numero: 3)), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroOrigemIncompativelComDespesa.Instancia);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_SaidaDeContaNaParcelada_Deve_RetornarParcelaIncompativel(
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        UnitResult<Erro> result = VinculoTransacaoService.Vincular(transacoes.DaConta(saida.AVista()), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroParcelaIncompativelComDespesa.Instancia);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_ParcelaNaSimples_Deve_RetornarParcelaIncompativel(
        Cartao cartao,
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DoCartao(cartao, saida.ComParcela());

        UnitResult<Erro> result = VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroParcelaIncompativelComDespesa.Instancia);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_ParcelaNaAdiada_Deve_RetornarParcelaIncompativel(
        [Frozen] Cartao cartao,
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DoCartao(cartao, saida.ComParcela());

        UnitResult<Erro> result = VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroParcelaIncompativelComDespesa.Instancia);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_SemParcelaNaParcelada_Deve_RetornarParcelaIncompativel(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        UnitResult<Erro> result = VinculoTransacaoService.Vincular(transacoes.DoCartao(cartao, saida.AVista()), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroParcelaIncompativelComDespesa.Instancia);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_ParcelaDeOutraData_Deve_RetornarParcelaIncompativel(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Lancamento compra = saida.DaCompra(despesa.DataOcorrencia.AddDays(1), despesa.Descricao, (Parcela)(3, despesa.Parcelas.Count));

        UnitResult<Erro> result = VinculoTransacaoService.Vincular(transacoes.DoCartao(cartao, compra), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroParcelaIncompativelComDespesa.Instancia);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_ParcelaDeOutraDescricao_Deve_RetornarParcelaIncompativel(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida,
        TextoObrigatorio outraDescricao)
    {
        Lancamento compra = saida.DaCompra(despesa.DataOcorrencia, outraDescricao, (Parcela)(3, despesa.Parcelas.Count));

        UnitResult<Erro> result = VinculoTransacaoService.Vincular(transacoes.DoCartao(cartao, compra), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroParcelaIncompativelComDespesa.Instancia);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_ParcelaDeOutroTotal_Deve_RetornarParcelaIncompativel(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Lancamento compra = saida.DaCompra(despesa.DataOcorrencia, despesa.Descricao, (Parcela)(3, despesa.Parcelas.Count + 2));

        UnitResult<Erro> result = VinculoTransacaoService.Vincular(transacoes.DoCartao(cartao, compra), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroParcelaIncompativelComDespesa.Instancia);
    }

    [Theory, DomainAutoData]
    public void Vincular_Quando_TransacaoDeOutroUsuario_Deve_LancarExcecao(
        DespesaAdiada despesa,
        [Frozen] IdUsuario outroUsuario,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DaConta(saida.AVista());
        transacao.IdUsuario.Should().Be(outroUsuario);

        Action act = () => VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("transacao");
    }

    [Theory, DomainAutoData]
    public void Vincular_Quando_DespesaQueUsaATransacaoDeOutroUsuario_Deve_LancarExcecao(
        DespesaAdiada deOutroUsuario,
        [Frozen] IdUsuario idUsuario,
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        deOutroUsuario.IdUsuario.Should().NotBe(idUsuario);
        Transacao transacao = transacoes.DaConta(saida.AVista());

        Action act = () => VinculoTransacaoService.Vincular(transacao, despesa, deOutroUsuario);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("despesaQueUsaATransacao");
    }

    [Theory, VinculoAutoData]
    public void Desvincular_Quando_SimplesPagaPelaSaida_Deve_VoltarParaPendente(
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Pagamento original = despesa.Pagamento;
        Transacao transacao = transacoes.DaConta(saida.AVista());
        VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        UnitResult<Erro> result = VinculoTransacaoService.Desvincular(transacao, despesa);

        result.Should().Succeed();
        despesa.Pagamento.Should().Be(original);
    }

    [Theory, VinculoAutoData]
    public void Desvincular_Quando_AdiadaCobrada_Deve_TirarACobranca(
        [Frozen] Cartao cartao,
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Pagamento original = despesa.Pagamento;
        Transacao transacao = transacoes.DoCartao(cartao, saida.AVista());
        VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        UnitResult<Erro> result = VinculoTransacaoService.Desvincular(transacao, despesa);

        result.Should().Succeed();
        despesa.Cobranca.Should().BeNull();
        despesa.Pagamento.Should().Be(original);
    }

    [Theory, VinculoAutoData]
    public void Desvincular_Quando_AdiadaSemCobranca_Deve_RetornarNaoCobrou(
        [Frozen] Cartao cartao,
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        UnitResult<Erro> result = VinculoTransacaoService.Desvincular(transacoes.DoCartao(cartao, saida.AVista()), despesa);

        result.Should().Fail().Which.Should().Be(ErroTransacaoNaoCobrouADespesa.Instancia);
    }

    [Theory, VinculoAutoData]
    public void Desvincular_Quando_AdiadaCobradaPorOutraCompra_Deve_RetornarNaoCobrouEManterACobranca(
        [Frozen] Cartao cartao,
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida,
        SaidaDeTeste outraSaida)
    {
        Transacao transacao = transacoes.DoCartao(cartao, saida.AVista());
        VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        UnitResult<Erro> result = VinculoTransacaoService.Desvincular(transacoes.DoCartao(cartao, outraSaida.AVista()), despesa);

        result.Should().Fail().Which.Should().Be(ErroTransacaoNaoCobrouADespesa.Instancia);
        despesa.Cobranca!.IdTransacao.Should().Be(transacao.Id);
    }

    [Theory, VinculoAutoData]
    public void Desvincular_Quando_SaidaDeContaNaAdiada_Deve_RetornarNaoPagou(
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        UnitResult<Erro> result = VinculoTransacaoService.Desvincular(transacoes.DaConta(saida.AVista()), despesa);

        result.Should().Fail().Which.Should().Be(ErroTransacaoNaoPagouADespesa.Instancia);
    }

    [Theory, VinculoAutoData]
    public void Desvincular_Quando_ParcelaCobrada_Deve_TirarSoACobrancaDela(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Pagamento[] originais = despesa.Parcelas.Select(p => p.Pagamento).ToArray();
        Transacao transacao = transacoes.DoCartao(cartao, saida.DaCompra(despesa, numero: 3));
        VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        UnitResult<Erro> result = VinculoTransacaoService.Desvincular(transacao, despesa);

        result.Should().Succeed();
        despesa.Parcelas.Should().OnlyContain(p => p.Cobranca == null);
        despesa.Parcelas.Select(p => p.Pagamento).Should().Equal(originais);
    }

    [Theory, VinculoAutoData]
    public void Desvincular_Quando_ParcelaNasceuPaga_Deve_TirarACobrancaEContinuarPaga(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Pagamento paga = despesa.Parcelas[0].Pagamento;
        Transacao transacao = transacoes.DoCartao(cartao, saida.DaCompra(despesa, numero: 1));
        VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        UnitResult<Erro> result = VinculoTransacaoService.Desvincular(transacao, despesa);

        result.Should().Succeed();
        despesa.Parcelas[0].Cobranca.Should().BeNull();
        despesa.Parcelas[0].Pagamento.Should().Be(paga);
    }

    [Theory, VinculoAutoData]
    public void Desvincular_Quando_AdiadaPagaPelaFatura_Deve_RetornarDespesaJaPagaEManterACobranca(
        [Frozen] Cartao cartao,
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste pagamento)
    {
        Transacao transacao = transacoes.DoCartao(cartao, compra.AVista());
        VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();
        PagamentoFaturaService.Pagar(transacoes.DaConta(pagamento.AVista()), cartao, despesa.MesReferencia, [cartao], [despesa], despesasPagasPelaSaida: []).Should().Succeed();
        Cobranca cobranca = despesa.Cobranca!;

        UnitResult<Erro> result = VinculoTransacaoService.Desvincular(transacao, despesa);

        result.Should().Fail().Which.Should().Be(ErroDespesaJaPaga.Instancia);
        despesa.Cobranca.Should().Be(cobranca);
    }

    [Theory, VinculoAutoData]
    public void Desvincular_Quando_ParcelaPagaPelaFatura_Deve_RetornarDespesaJaPagaEManterACobranca(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste pagamento)
    {
        Transacao transacao = transacoes.DoCartao(cartao, compra.DaCompra(despesa, numero: 3));
        VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();
        PagamentoFaturaService.Pagar(transacoes.DaConta(pagamento.AVista()), cartao, despesa.Parcelas[2].MesReferencia, [cartao], [despesa], despesasPagasPelaSaida: []).Should().Succeed();
        Cobranca cobranca = despesa.Parcelas[2].Cobranca!;

        UnitResult<Erro> result = VinculoTransacaoService.Desvincular(transacao, despesa);

        result.Should().Fail().Which.Should().Be(ErroDespesaJaPaga.Instancia);
        despesa.Parcelas[2].Cobranca.Should().Be(cobranca);
    }

    [Theory, VinculoAutoData]
    public void Desvincular_Quando_ParceladaSemACompra_Deve_RetornarNaoCobrou(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        UnitResult<Erro> result = VinculoTransacaoService.Desvincular(transacoes.DoCartao(cartao, saida.DaCompra(despesa, numero: 3)), despesa);

        result.Should().Fail().Which.Should().Be(ErroTransacaoNaoCobrouADespesa.Instancia);
    }

    [Theory, VinculoAutoData]
    public void Desvincular_Quando_ParcelaCobradaPorOutraCompra_Deve_RetornarNaoCobrouEManterACobranca(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida,
        SaidaDeTeste outraSaida)
    {
        Transacao transacao = transacoes.DoCartao(cartao, saida.DaCompra(despesa, numero: 3));
        VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();
        Transacao outra = transacoes.DoCartao(cartao, outraSaida.DaCompra(despesa, numero: 3));

        UnitResult<Erro> result = VinculoTransacaoService.Desvincular(outra, despesa);

        result.Should().Fail().Which.Should().Be(ErroTransacaoNaoCobrouADespesa.Instancia);
        despesa.Parcelas[2].Cobranca!.IdTransacao.Should().Be(transacao.Id);
    }

    [Theory, VinculoAutoData]
    public void Desvincular_Quando_SimplesPendente_Deve_RetornarNaoPagou(
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        UnitResult<Erro> result = VinculoTransacaoService.Desvincular(transacoes.DaConta(saida.AVista()), despesa);

        result.Should().Fail().Which.Should().Be(ErroTransacaoNaoPagouADespesa.Instancia);
    }

    [Theory, VinculoAutoData]
    public void Desvincular_Quando_SimplesPagaPorOutraSaida_Deve_RetornarNaoPagouEContinuarPaga(
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida,
        SaidaDeTeste outraSaida)
    {
        Transacao transacao = transacoes.DaConta(saida.AVista());
        VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        UnitResult<Erro> result = VinculoTransacaoService.Desvincular(transacoes.DaConta(outraSaida.AVista()), despesa);

        result.Should().Fail().Which.Should().Be(ErroTransacaoNaoPagouADespesa.Instancia);
        despesa.Pagamento.IdTransacao.Should().Be(transacao.Id);
    }

    [Theory, VinculoAutoData]
    public void Desvincular_Quando_CompraDoCartaoNaSimples_Deve_RetornarNaoCobrou(
        Cartao cartao,
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        UnitResult<Erro> result = VinculoTransacaoService.Desvincular(transacoes.DoCartao(cartao, saida.AVista()), despesa);

        result.Should().Fail().Which.Should().Be(ErroTransacaoNaoCobrouADespesa.Instancia);
    }

    [Theory, DomainAutoData]
    public void Desvincular_Quando_TransacaoDeOutroUsuario_Deve_LancarExcecao(
        DespesaSimples despesa,
        [Frozen] IdUsuario outroUsuario,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DaConta(saida.AVista());
        transacao.IdUsuario.Should().Be(outroUsuario);

        Action act = () => VinculoTransacaoService.Desvincular(transacao, despesa);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("transacao");
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_InteiraNasceuPagaSemTransacao_Deve_AceitarOVinculo(
        DespesaSimples modelo,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        // O fixture entrega a despesa pendente: aqui a mesma despesa nasce paga, pelo Criar.
        DespesaSimples despesa = DespesaSimples
            .Criar(modelo.IdUsuario, modelo.IdBeneficiario, modelo.Descricao, modelo.IdCategoria, modelo.DataOcorrencia, modelo.FormaPagamento, modelo.Pagamento.ValorProjetado)
            .Value;
        Transacao transacao = transacoes.DaConta(saida.AVista());

        UnitResult<Erro> result = VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Succeed();
        despesa.Pagamento.IdTransacao.Should().Be(transacao.Id);
        despesa.Pagamento.ValorEfetivo.Should().Be(saida.ValorPago);
    }

    [Theory, VinculoAutoData]
    public void PagamentosRealizadosPor_Quando_SimplesPagaPelaTransacao_Deve_RetornarOPagamentoDaDespesa(
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DaConta(saida.AVista());
        VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        IReadOnlyList<Pagamento> result = despesa.PagamentosRealizadosPor(transacao.Id);

        result.Should().Equal(despesa.Pagamento);
    }

    [Theory, VinculoAutoData]
    public void PagamentosRealizadosPor_Quando_InteiraPendente_Deve_RetornarVazio(DespesaSimples despesa, IdTransacao idTransacao)
    {
        IReadOnlyList<Pagamento> result = despesa.PagamentosRealizadosPor(idTransacao);

        result.Should().BeEmpty();
    }

    [Theory, VinculoAutoData]
    public void PagamentosRealizadosPor_Quando_InteiraPagaPorOutraTransacao_Deve_RetornarVazio(
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida,
        IdTransacao outraTransacao)
    {
        VinculoTransacaoService.Vincular(transacoes.DaConta(saida.AVista()), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        IReadOnlyList<Pagamento> result = despesa.PagamentosRealizadosPor(outraTransacao);

        result.Should().BeEmpty();
    }

    [Theory, VinculoAutoData]
    public void PagamentosRealizadosPor_Quando_AdiadaCobradaPelaTransacao_Deve_RetornarVazio(
        [Frozen] Cartao cartao,
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DoCartao(cartao, saida.AVista());
        VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        IReadOnlyList<Pagamento> result = despesa.PagamentosRealizadosPor(transacao.Id);

        result.Should().BeEmpty();
    }

    [Theory, VinculoAutoData]
    public void PagamentoCobradoPor_Quando_AdiadaCobradaPelaTransacao_Deve_RetornarOPagamentoDaDespesa(
        [Frozen] Cartao cartao,
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DoCartao(cartao, saida.AVista());
        VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        Maybe<Pagamento> result = despesa.PagamentoCobradoPor(transacao.Id);

        result.Should().HaveValue(despesa.Pagamento);
    }

    [Theory, VinculoAutoData]
    public void PagamentoCobradoPor_Quando_AdiadaCobradaPorOutraTransacao_Deve_RetornarVazio(
        [Frozen] Cartao cartao,
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida,
        IdTransacao outraTransacao)
    {
        VinculoTransacaoService.Vincular(transacoes.DoCartao(cartao, saida.AVista()), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        Maybe<Pagamento> result = despesa.PagamentoCobradoPor(outraTransacao);

        result.Should().HaveNoValue();
    }

    [Theory, VinculoAutoData]
    public void PagamentoCobradoPor_Quando_SimplesPagaPelaTransacao_Deve_RetornarVazio(
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DaConta(saida.AVista());
        VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        Maybe<Pagamento> result = despesa.PagamentoCobradoPor(transacao.Id);

        result.Should().HaveNoValue();
    }

    [Theory, VinculoAutoData]
    public void PagamentoCobradoPor_Quando_ParcelaCobradaPelaTransacao_Deve_RetornarOPagamentoDaParcela(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DoCartao(cartao, saida.DaCompra(despesa, numero: 3));
        VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        Maybe<Pagamento> result = despesa.PagamentoCobradoPor(transacao.Id);

        result.Should().HaveValue(despesa.Parcelas[2].Pagamento);
    }

    [Theory, VinculoAutoData]
    public void ParcelaCobradaPor_Quando_ParcelaCobradaPelaTransacao_Deve_RetornarAParcela(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DoCartao(cartao, saida.DaCompra(despesa, numero: 3));
        VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        Maybe<ParcelaDespesa> result = despesa.ParcelaCobradaPor(transacao.Id);

        result.Should().HaveValue(despesa.Parcelas[2]);
    }

    [Theory, VinculoAutoData]
    public void ParcelaCobradaPor_Quando_TransacaoNaoCobrouParcela_Deve_RetornarVazio(DespesaParcelada despesa, IdTransacao idTransacao)
    {
        Maybe<ParcelaDespesa> result = despesa.ParcelaCobradaPor(idTransacao);

        result.Should().HaveNoValue();
    }

    [Theory, VinculoAutoData]
    public void ParcelaCobradaPor_Quando_CobrancaDesfeita_Deve_RetornarVazio(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DoCartao(cartao, saida.DaCompra(despesa, numero: 3));
        VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();
        VinculoTransacaoService.Desvincular(transacao, despesa).Should().Succeed();

        Maybe<ParcelaDespesa> result = despesa.ParcelaCobradaPor(transacao.Id);

        result.Should().HaveNoValue();
    }

    [Theory, VinculoAutoData]
    public void ParcelasPagasPor_Quando_ParcelaCobradaPelaTransacao_Deve_RetornarVazio(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DoCartao(cartao, saida.DaCompra(despesa, numero: 3));
        VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        IReadOnlyList<ParcelaDespesa> result = despesa.ParcelasPagasPor(transacao.Id);

        result.Should().BeEmpty();
    }

    [Theory, VinculoAutoData]
    public void ValorVigenteDoMes_Quando_AdiadaCobrada_Deve_RetornarOValorCobrado(
        [Frozen] Cartao cartao,
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        VinculoTransacaoService.Vincular(transacoes.DoCartao(cartao, saida.AVista()), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        Maybe<ReaisPositivo> result = despesa.ValorVigenteDoMes(despesa.MesReferencia);

        result.Should().HaveValue(saida.ValorPago);
    }

    [Theory, VinculoAutoData]
    public void ValorVigenteDoMes_Quando_ParcelaPendenteCobrada_Deve_RetornarOValorCobrado(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        VinculoTransacaoService.Vincular(transacoes.DoCartao(cartao, saida.DaCompra(despesa, numero: 3)), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        Maybe<ReaisPositivo> result = despesa.ValorVigenteDoMes(despesa.Parcelas[2].MesReferencia);

        result.Should().HaveValue(saida.ValorPago);
    }

    [Theory, VinculoAutoData]
    public void ValorVigenteDoMes_Quando_ParcelaNasceuPagaECobrada_Deve_RetornarOValorEfetivo(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        ParcelaDespesa primeira = despesa.Parcelas[0];
        VinculoTransacaoService.Vincular(transacoes.DoCartao(cartao, saida.DaCompra(despesa, numero: 1)), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        Maybe<ReaisPositivo> result = despesa.ValorVigenteDoMes(primeira.MesReferencia);

        result.Should().HaveValue(primeira.Pagamento.ValorEfetivo!);
    }

    [Theory, VinculoAutoData]
    public void ValorVigenteDoMes_Quando_SimplesPagaComOutroValor_Deve_RetornarOValorEfetivo(
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        VinculoTransacaoService.Vincular(transacoes.DaConta(saida.AVista()), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        Maybe<ReaisPositivo> result = despesa.ValorVigenteDoMes(despesa.MesReferencia);

        result.Should().HaveValue(saida.ValorPago);
    }

    /// <summary>Uma saída de dinheiro e os lançamentos que os testes derivam dela, com o valor nunca zerado.</summary>
    public sealed class SaidaDeTeste
    {
        private readonly DateOnly _data;
        private readonly decimal _valor;
        private readonly TextoObrigatorio _descricao;
        private readonly IdentidadeLancamento _identidade;

        public ReaisPositivo ValorPago => (ReaisPositivo)decimal.Abs(_valor);

        public SaidaDeTeste(DateOnly data, ReaisPositivo valor, TextoObrigatorio descricao, IdentidadeLancamento identidade)
        {
            _data = data;
            _valor = -(valor + 1m);
            _descricao = descricao;
            _identidade = identidade;
        }

        public Lancamento AVista() => Criar(_data, _valor, _descricao, parcela: null);

        public Lancamento ComParcela() => Criar(_data, _valor, _descricao, _parcelaDaCompra);

        public Lancamento ComValor(decimal valor) => Criar(_data, valor, _descricao, parcela: null);

        public Lancamento DaCompra(DespesaParcelada despesa, int numero) =>
            Criar(despesa.DataOcorrencia, _valor, despesa.Descricao, (Parcela)(numero, despesa.Parcelas.Count));

        /// <summary>Uma parcela com a chave escolhida pelo teste, para os casos de outra compra.</summary>
        public Lancamento DaCompra(DateOnly data, TextoObrigatorio descricao, Parcela parcela) => Criar(data, _valor, descricao, parcela);

        private Lancamento Criar(DateOnly data, decimal valor, TextoObrigatorio descricao, Parcela? parcela) =>
            Lancamento.DoCartao(data, hora: null, valor, descricao, contraparte: null, parcela, categoriaBanco: null, _identidade);
    }

    /// <summary>
    /// Tudo sai do mesmo usuário, e a parcelada, na parcela 3 de 10, que tem parcelas pagas e pendentes.
    /// </summary>
    private sealed class VinculoAutoDataAttribute : DomainAutoDataAttribute
    {
        public VinculoAutoDataAttribute()
            : base(fixture =>
            {
                fixture.Freeze<IdUsuario>();
                fixture.Register(() => _parcelaDaCompra);
            })
        {
        }
    }

    private sealed class InlineVinculoAutoDataAttribute : InlineAutoDataAttribute
    {
        public InlineVinculoAutoDataAttribute(params object[] valores)
            : base(new VinculoAutoDataAttribute(), valores)
        {
        }
    }
}
