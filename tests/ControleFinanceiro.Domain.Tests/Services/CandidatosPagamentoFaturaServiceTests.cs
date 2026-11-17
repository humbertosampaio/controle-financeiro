using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Services;

public sealed class CandidatosPagamentoFaturaServiceTests
{
    // Os valores são o assunto: o total é calculado à mão.
    [Theory, CandidatosAutoData]
    public void Apontar_Quando_SaidaComOTotalNoVencimento_Deve_ApontarAFaturaComOTotal(
        [Frozen] VencimentoFatura vencimento,
        Cartao cartao,
        CenarioPagamentoFatura cenario,
        LancamentoConta compra,
        LancamentoConta outraCompra,
        LancamentoConta estorno,
        LancamentoConta linhaSaida)
    {
        cenario.ImportarFatura(cartao, vencimento, compra.NoCartaoComValor(-100m), outraCompra.NoCartaoComValor(-50.25m), estorno.NoCartaoComValor(20m));
        Transacao saida = cenario.Saida(linhaSaida, vencimento, (ReaisPositivo)130.25m);

        IReadOnlyList<CandidatoPagamentoFatura> result = cenario.Apontar([cartao], vencimento.Mes);

        CandidatoPagamentoFatura candidato = result.Should().ContainSingle().Which;
        candidato.IdSaida.Should().Be(saida.Id);
        candidato.FaturaCartaoAberta.Fatura.Should().Be(FaturaCartao.DoCartao(cartao.Id, [cartao], vencimento.Mes));
        candidato.FaturaCartaoAberta.Vencimento.Should().Be(vencimento);
        candidato.FaturaCartaoAberta.Total.Should().Be((ReaisPositivo)130.25m);
    }

    [Theory, CandidatosAutoData]
    public void Apontar_Quando_DoisFinaisDaMesmaFatura_Deve_SomarOsDoisEmUmCandidatoApenas(
        [Frozen] VencimentoFatura vencimento,
        Cartao cartao,
        SufixoCartao outroSufixo,
        CenarioPagamentoFatura cenario,
        LancamentoConta compra,
        LancamentoConta compraOutroFinal,
        LancamentoConta linhaSaida)
    {
        Cartao outroFinal = cartao.OutroFinal(outroSufixo);
        cenario.ImportarFatura(cartao, vencimento, compra.NoCartaoComValor(-100m));
        cenario.ImportarFatura(outroFinal, vencimento, compraOutroFinal.NoCartaoComValor(-30m));
        cenario.Saida(linhaSaida, vencimento, (ReaisPositivo)130m);

        IReadOnlyList<CandidatoPagamentoFatura> result = cenario.Apontar([cartao, outroFinal], vencimento.Mes);

        result.Should().ContainSingle().Which.FaturaCartaoAberta.Total.Should().Be((ReaisPositivo)130m);
    }

    [Theory, CandidatosAutoData]
    public void Apontar_Quando_PagamentoFaturaAnteriorNaFatura_Deve_DeixarOPagamentoAnteriorForaDoTotal(
        [Frozen] VencimentoFatura vencimento,
        Cartao cartao,
        CenarioPagamentoFatura cenario,
        LancamentoConta compra,
        LancamentoCartao pagamentoAnterior,
        LancamentoConta linhaSaida)
    {
        cenario.ImportarFatura(cartao, vencimento, compra.NoCartaoComValor(-100m), pagamentoAnterior.SemCategoria().ComoPagamentoFaturaAnterior());
        cenario.Saida(linhaSaida, vencimento, (ReaisPositivo)100m);

        IReadOnlyList<CandidatoPagamentoFatura> result = cenario.Apontar([cartao], vencimento.Mes);

        result.Should().ContainSingle().Which.FaturaCartaoAberta.Total.Should().Be((ReaisPositivo)100m);
    }

    [Theory, CandidatosAutoData]
    public void Apontar_Quando_ParcelaDeCompraAntigaNaFatura_Deve_EntrarNoTotal(
        [Frozen] VencimentoFatura vencimento,
        Cartao cartao,
        CenarioPagamentoFatura cenario,
        LancamentoConta compra,
        LancamentoCartao parcela,
        LancamentoConta linhaSaida)
    {
        LancamentoCartao parcelaCompraAntiga = parcela.NaFatura(vencimento.Value.AddMonths(-6), (Parcela)(7, 10));
        cenario.ImportarFatura(cartao, vencimento, compra.NoCartaoComValor(-100m), parcelaCompraAntiga.NoCartaoComValor(-40m));
        cenario.Saida(linhaSaida, vencimento, (ReaisPositivo)140m);

        IReadOnlyList<CandidatoPagamentoFatura> result = cenario.Apontar([cartao], vencimento.Mes);

        result.Should().ContainSingle().Which.FaturaCartaoAberta.Total.Should().Be((ReaisPositivo)140m);
    }

    [Theory, CandidatosAutoData]
    public void Apontar_Quando_CompraDaFaturaDeOutroMes_Deve_DeixarACompraDoOutroMesForaDoTotal(
        [Frozen] VencimentoFatura vencimento,
        Cartao cartao,
        CenarioPagamentoFatura cenario,
        LancamentoConta compra,
        LancamentoConta compraMesSeguinte,
        LancamentoConta linhaSaida)
    {
        cenario.ImportarFatura(cartao, vencimento, compra.NoCartaoComValor(-100m));
        cenario.ImportarFatura(cartao, (VencimentoFatura)vencimento.Value.AddMonths(1), compraMesSeguinte.NoCartaoComValor(-30m));
        cenario.Saida(linhaSaida, vencimento, (ReaisPositivo)100m);

        IReadOnlyList<CandidatoPagamentoFatura> result = cenario.Apontar([cartao], vencimento.Mes);

        result.Should().ContainSingle().Which.FaturaCartaoAberta.Total.Should().Be((ReaisPositivo)100m);
    }

    // Com o estorno de 30, a compra de 30 zera o total, e a de 10 o deixa em −20, cujo valor sem sinal é o da saída.
    [Theory]
    [InlineCandidatosAutoData(-30)]
    [InlineCandidatosAutoData(-10)]
    public void Apontar_Quando_FaturaSemTotalPositivo_Deve_FicarSemCandidato(
        decimal valorCompra,
        [Frozen] VencimentoFatura vencimento,
        Cartao cartao,
        CenarioPagamentoFatura cenario,
        LancamentoConta compra,
        LancamentoConta estorno,
        LancamentoConta linhaSaida)
    {
        cenario.ImportarFatura(cartao, vencimento, compra.NoCartaoComValor(valorCompra), estorno.NoCartaoComValor(30m));
        cenario.Saida(linhaSaida, vencimento, (ReaisPositivo)20m);

        IReadOnlyList<CandidatoPagamentoFatura> result = cenario.Apontar([cartao], vencimento.Mes);

        result.Should().BeEmpty();
    }

    [Theory, CandidatosAutoData]
    public void Apontar_Quando_SaidaUmCentavoDiferente_Deve_FicarSemCandidato(
        [Frozen] VencimentoFatura vencimento,
        Cartao cartao,
        CenarioPagamentoFatura cenario,
        LancamentoConta compra,
        LancamentoConta linhaSaida)
    {
        cenario.ImportarFatura(cartao, vencimento, compra.NoCartaoComValor(-100m));
        cenario.Saida(linhaSaida, vencimento, (ReaisPositivo)100.01m);

        IReadOnlyList<CandidatoPagamentoFatura> result = cenario.Apontar([cartao], vencimento.Mes);

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineCandidatosAutoData(-VencimentoFatura.DiasAntesVencimento)]
    [InlineCandidatosAutoData(VencimentoFatura.DiasDepoisVencimento)]
    public void Apontar_Quando_SaidaNaPontaDaJanela_Deve_ApontarOCandidato(
        int diasVencimento,
        [Frozen] VencimentoFatura vencimento,
        Cartao cartao,
        CenarioPagamentoFatura cenario,
        LancamentoConta compra,
        LancamentoConta linhaSaida)
    {
        cenario.ImportarFatura(cartao, vencimento, compra.NoCartaoComValor(-100m));
        cenario.Saida(linhaSaida, vencimento.Value.AddDays(diasVencimento), (ReaisPositivo)100m);

        IReadOnlyList<CandidatoPagamentoFatura> result = cenario.Apontar([cartao], vencimento.Mes);

        result.Should().ContainSingle();
    }

    [Theory]
    [InlineCandidatosAutoData(-VencimentoFatura.DiasAntesVencimento - 1)]
    [InlineCandidatosAutoData(VencimentoFatura.DiasDepoisVencimento + 1)]
    public void Apontar_Quando_SaidaForaDaJanela_Deve_FicarSemCandidato(
        int diasVencimento,
        [Frozen] VencimentoFatura vencimento,
        Cartao cartao,
        CenarioPagamentoFatura cenario,
        LancamentoConta compra,
        LancamentoConta linhaSaida)
    {
        cenario.ImportarFatura(cartao, vencimento, compra.NoCartaoComValor(-100m));
        cenario.Saida(linhaSaida, vencimento.Value.AddDays(diasVencimento), (ReaisPositivo)100m);

        IReadOnlyList<CandidatoPagamentoFatura> result = cenario.Apontar([cartao], vencimento.Mes);

        result.Should().BeEmpty();
    }

    // A compra do outro cartão é uma saída com o valor do total, no dia do vencimento, mas não é da conta.
    [Theory, CandidatosAutoData]
    public void Apontar_Quando_ApenasSaidaNoCartaoComOTotal_Deve_FicarSemCandidato(
        [Frozen] VencimentoFatura vencimento,
        Cartao cartao,
        CenarioPagamentoFatura cenario,
        LancamentoConta compra,
        LancamentoCartao compraOutroBanco)
    {
        Cartao cartaoOutroBanco = cartao.EmOutroBanco();
        LancamentoCartao compraOutroCartaoNoVencimento = compraOutroBanco.NaFatura(vencimento, parcela: null).NoCartaoComValor(-100m);
        cenario.ImportarFatura(cartao, vencimento, compra.NoCartaoComValor(-100m));
        cenario.ImportarFatura(cartaoOutroBanco, (VencimentoFatura)vencimento.Value.AddMonths(1), compraOutroCartaoNoVencimento);

        IReadOnlyList<CandidatoPagamentoFatura> result = cenario.Apontar([cartao, cartaoOutroBanco], vencimento.Mes);

        result.Should().BeEmpty();
    }

    [Theory, CandidatosAutoData]
    public void Apontar_Quando_DuasSaidasComOTotal_Deve_FicarSemCandidato(
        [Frozen] VencimentoFatura vencimento,
        Cartao cartao,
        CenarioPagamentoFatura cenario,
        LancamentoConta compra,
        LancamentoConta linhaSaida,
        LancamentoConta outraLinhaSaida)
    {
        cenario.ImportarFatura(cartao, vencimento, compra.NoCartaoComValor(-100m));
        cenario.Saida(linhaSaida, vencimento, (ReaisPositivo)100m);
        cenario.Saida(outraLinhaSaida, vencimento.Value.AddDays(1), (ReaisPositivo)100m);

        IReadOnlyList<CandidatoPagamentoFatura> result = cenario.Apontar([cartao], vencimento.Mes);

        result.Should().BeEmpty();
    }

    [Theory, CandidatosAutoData]
    public void Apontar_Quando_DuasFaturasComOValorDaSaida_Deve_FicarSemCandidato(
        [Frozen] VencimentoFatura vencimento,
        Cartao cartao,
        CenarioPagamentoFatura cenario,
        LancamentoConta compra,
        LancamentoConta compraOutroBanco,
        LancamentoConta linhaSaida)
    {
        Cartao cartaoOutroBanco = cartao.EmOutroBanco();
        cenario.ImportarFatura(cartao, vencimento, compra.NoCartaoComValor(-100m));
        cenario.ImportarFatura(cartaoOutroBanco, vencimento, compraOutroBanco.NoCartaoComValor(-100m));
        cenario.Saida(linhaSaida, vencimento, (ReaisPositivo)100m);

        IReadOnlyList<CandidatoPagamentoFatura> result = cenario.Apontar([cartao, cartaoOutroBanco], vencimento.Mes);

        result.Should().BeEmpty();
    }

    [Theory, CandidatosAutoData]
    public void Apontar_Quando_SaidaJaPagouUmaSimples_Deve_FicarSemCandidato(
        [Frozen] VencimentoFatura vencimento,
        Cartao cartao,
        CenarioPagamentoFatura cenario,
        LancamentoConta compra,
        LancamentoConta linhaSaida,
        DespesaSimples simples)
    {
        cenario.ImportarFatura(cartao, vencimento, compra.NoCartaoComValor(-100m));
        Transacao saida = cenario.Saida(linhaSaida, vencimento, (ReaisPositivo)100m);
        VinculoTransacaoService.Vincular(saida, simples, despesaQueUsaATransacao: Maybe<Despesa>.None, cartoesDoUsuario: [cartao]).Should().Succeed();

        IReadOnlyList<CandidatoPagamentoFatura> result = cenario.Apontar([cartao], vencimento.Mes, despesasMeses: [], despesasPagasSaidas: [simples]);

        result.Should().BeEmpty();
    }

    [Theory, CandidatosAutoData]
    public void Apontar_Quando_FaturaJaPaga_Deve_FicarSemCandidato(
        [Frozen] VencimentoFatura vencimento,
        [Frozen] Cartao cartao,
        CenarioPagamentoFatura cenario,
        DespesaAdiada adiada,
        LancamentoConta compra,
        LancamentoConta linhaPagamento,
        LancamentoConta linhaSaida)
    {
        Transacao cobranca = cenario.ImportarFatura(cartao, vencimento, compra.NoCartaoComValor(-100m)).Single();
        VinculoTransacaoService.Vincular(cobranca, adiada, despesaQueUsaATransacao: Maybe<Despesa>.None, cartoesDoUsuario: [cartao]).Should().Succeed();
        Transacao pagamento = cenario.Saida(linhaPagamento, vencimento.Value.AddDays(-1), (ReaisPositivo)100m);
        PagamentoFaturaService.Pagar(pagamento, cartao, vencimento.Mes, [cartao], [adiada], despesasPagasPelaSaida: []).Should().Succeed();
        cenario.Saida(linhaSaida, vencimento, (ReaisPositivo)100m);

        IReadOnlyList<CandidatoPagamentoFatura> result = cenario.Apontar([cartao], vencimento.Mes, despesasMeses: [adiada], despesasPagasSaidas: [adiada]);

        result.Should().BeEmpty();
    }

    // Abril, pedido antes, confere de março a maio, e março, de fevereiro a abril: os meses saem em ordem, sem repetir.
    [Fact]
    public void MesesConferidos_Quando_MesesVizinhosForaDeOrdem_Deve_JuntarOsMesesEmOrdemSemRepetir()
    {
        IReadOnlyList<MesReferencia> result = CandidatosPagamentoFaturaService.MesesConferidos([(MesReferencia)(4, 2026), (MesReferencia)(3, 2026)]);

        result.Should().Equal((MesReferencia)(2, 2026), (MesReferencia)(3, 2026), (MesReferencia)(4, 2026), (MesReferencia)(5, 2026));
    }

    // As datas são o assunto: a saída de 30/03 cabe na janela da fatura que vence em 28/03 e na da que vence em 02/04.
    [Theory, CandidatosAutoData]
    public void Apontar_Quando_SaidaCabeNaFaturaDoMesVizinho_Deve_FicarSemCandidato(
        Cartao cartao,
        CenarioPagamentoFatura cenario,
        LancamentoConta compra,
        LancamentoConta compraOutroBanco,
        LancamentoConta linhaSaida)
    {
        Cartao cartaoOutroBanco = cartao.EmOutroBanco();
        cenario.ImportarFatura(cartao, (VencimentoFatura)new DateOnly(2026, 3, 28), compra.NoCartaoComValor(-100m));
        cenario.ImportarFatura(cartaoOutroBanco, (VencimentoFatura)new DateOnly(2026, 4, 2), compraOutroBanco.NoCartaoComValor(-100m));
        cenario.Saida(linhaSaida, new DateOnly(2026, 3, 30), (ReaisPositivo)100m);

        IReadOnlyList<CandidatoPagamentoFatura> result = cenario.Apontar([cartao, cartaoOutroBanco], (MesReferencia)(3, 2026));

        result.Should().BeEmpty();
    }

    [Theory, CandidatosAutoData]
    public void Apontar_Quando_CandidatoDoMesVizinho_Deve_DevolverApenasOsDosMesesPedidos(
        Cartao cartao,
        CenarioPagamentoFatura cenario,
        LancamentoConta compra,
        LancamentoConta linhaSaida)
    {
        cenario.ImportarFatura(cartao, (VencimentoFatura)new DateOnly(2026, 4, 2), compra.NoCartaoComValor(-100m));
        cenario.Saida(linhaSaida, new DateOnly(2026, 3, 30), (ReaisPositivo)100m);

        IReadOnlyList<CandidatoPagamentoFatura> result = cenario.Apontar([cartao], (MesReferencia)(3, 2026));

        result.Should().BeEmpty();
    }

    [Theory, CandidatosAutoData]
    public void Apontar_Quando_FaturaSemImportacaoNoMes_Deve_FicarSemCandidato(
        [Frozen] VencimentoFatura vencimento,
        Cartao cartao,
        CenarioPagamentoFatura cenario,
        LancamentoConta linhaSaida)
    {
        cenario.Saida(linhaSaida, vencimento, (ReaisPositivo)100m);

        IReadOnlyList<CandidatoPagamentoFatura> result = cenario.Apontar([cartao], vencimento.Mes);

        result.Should().BeEmpty();
    }

    [Theory, CandidatosAutoData]
    public void Apontar_Quando_CartaoDeOutroUsuario_Deve_LancarExcecao(Cartao cartaoOutroUsuario, MesReferencia mes)
    {
        Action act = () => CandidatosPagamentoFaturaService.Apontar(
            IdUsuario.FromNewVersion7Guid(),
            [mes],
            [cartaoOutroUsuario],
            importacoesFaturas: [],
            transacoesFaturas: [],
            saidasConta: [],
            despesasMeses: [],
            despesasPagasSaidas: []);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("cartoesUsuario");
    }

    [Theory, CandidatosAutoData]
    public void Apontar_Quando_ImportacaoDeOutroUsuario_Deve_LancarExcecao(ImportacaoFatura importacaoOutroUsuario, MesReferencia mes)
    {
        Action act = () => CandidatosPagamentoFaturaService.Apontar(
            IdUsuario.FromNewVersion7Guid(),
            [mes],
            cartoesUsuario: [],
            [importacaoOutroUsuario],
            transacoesFaturas: [],
            saidasConta: [],
            despesasMeses: [],
            despesasPagasSaidas: []);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("importacoesFaturas");
    }

    [Theory, CandidatosAutoData]
    public void Apontar_Quando_TransacaoFaturaDeOutroUsuario_Deve_LancarExcecao(
        TransacoesDeTeste transacoes,
        Cartao cartao,
        LancamentoCartao linha,
        MesReferencia mes)
    {
        TransacaoCartao transacaoOutroUsuario = transacoes.DoCartao(cartao, linha.SemCategoria());

        Action act = () => CandidatosPagamentoFaturaService.Apontar(
            IdUsuario.FromNewVersion7Guid(),
            [mes],
            cartoesUsuario: [],
            importacoesFaturas: [],
            [transacaoOutroUsuario],
            saidasConta: [],
            despesasMeses: [],
            despesasPagasSaidas: []);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("transacoesFaturas");
    }

    [Theory, CandidatosAutoData]
    public void Apontar_Quando_SaidaContaDeOutroUsuario_Deve_LancarExcecao(TransacoesDeTeste transacoes, LancamentoConta linha, MesReferencia mes)
    {
        TransacaoConta saidaOutroUsuario = transacoes.DaConta(linha.SemCategoria());

        Action act = () => CandidatosPagamentoFaturaService.Apontar(
            IdUsuario.FromNewVersion7Guid(),
            [mes],
            cartoesUsuario: [],
            importacoesFaturas: [],
            transacoesFaturas: [],
            [saidaOutroUsuario],
            despesasMeses: [],
            despesasPagasSaidas: []);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("saidasConta");
    }

    [Theory, CandidatosAutoData]
    public void Apontar_Quando_DespesaDoMesDeOutroUsuario_Deve_LancarExcecao(DespesaSimples despesaOutroUsuario, MesReferencia mes)
    {
        Action act = () => CandidatosPagamentoFaturaService.Apontar(
            IdUsuario.FromNewVersion7Guid(),
            [mes],
            cartoesUsuario: [],
            importacoesFaturas: [],
            transacoesFaturas: [],
            saidasConta: [],
            [despesaOutroUsuario],
            despesasPagasSaidas: []);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("despesasMeses");
    }

    [Theory, CandidatosAutoData]
    public void Apontar_Quando_DespesaPagaDeOutroUsuario_Deve_LancarExcecao(DespesaSimples despesaOutroUsuario, MesReferencia mes)
    {
        Action act = () => CandidatosPagamentoFaturaService.Apontar(
            IdUsuario.FromNewVersion7Guid(),
            [mes],
            cartoesUsuario: [],
            importacoesFaturas: [],
            transacoesFaturas: [],
            saidasConta: [],
            despesasMeses: [],
            [despesaOutroUsuario]);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("despesasPagasSaidas");
    }

    /// <summary>Tudo sai do mesmo usuário e na mesma data, para a despesa adiada vencer no mês da fatura.</summary>
    private sealed class CandidatosAutoDataAttribute : DomainAutoDataAttribute
    {
        public CandidatosAutoDataAttribute()
            : base(fixture =>
            {
                fixture.Freeze<IdUsuario>();
                fixture.Freeze<DateOnly>();
            })
        {
        }
    }

    private sealed class InlineCandidatosAutoDataAttribute : InlineAutoDataAttribute
    {
        public InlineCandidatosAutoDataAttribute(params object[] valores)
            : base(new CandidatosAutoDataAttribute(), valores)
        {
        }
    }
}
