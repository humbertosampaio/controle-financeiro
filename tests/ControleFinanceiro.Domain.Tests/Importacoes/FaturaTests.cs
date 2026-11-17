using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Importacoes;

public sealed class FaturaTests
{
    [Theory, DomainAutoData]
    public void Criar_Quando_SemLancamentos_Deve_RetornarFalha(Fatura modelo)
    {
        Result<Fatura> result = Fatura.Criar(modelo.Banco, modelo.Vencimento, lancamentos: []);

        result.Should().FailWith("A fatura não traz lançamentos.");
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_GuardarOBancoOVencimentoEOsLancamentosNaOrdemDoArquivo(
        Banco banco,
        VencimentoFatura vencimento,
        LancamentoFatura primeiro,
        LancamentoFatura segundo)
    {
        Result<Fatura> result = Fatura.Criar(banco, vencimento, [primeiro, segundo]);

        Fatura fatura = result.Should().Succeed().Subject;
        fatura.Banco.Should().Be(banco);
        fatura.Vencimento.Should().Be(vencimento);
        fatura.Lancamentos.Should().Equal(primeiro, segundo);
    }

    [Theory, DomainAutoData]
    public void Sufixos_Deve_TrazerOsFinaisDistintosNaOrdemDaPrimeiraAparicao(
        Fatura modelo,
        LancamentoCartao lancamento,
        SufixoCartao fisico,
        SufixoCartao virtualCartao)
    {
        Fatura sut = Fatura.Criar(
            modelo.Banco,
            modelo.Vencimento,
            [
                LancamentoFatura.Criar(lancamento, virtualCartao),
                LancamentoFatura.Criar(lancamento, fisico),
                LancamentoFatura.Criar(lancamento, virtualCartao),
            ]).Value;

        sut.Sufixos.Should().Equal(virtualCartao, fisico);
    }

    [Theory, DomainAutoData]
    public void LancamentosCartao_Deve_TrazerSoOsDoFinalNaOrdemDoArquivo(
        Fatura modelo,
        LancamentoCartao primeiro,
        LancamentoCartao doOutroCartao,
        LancamentoCartao segundo,
        SufixoCartao sufixo,
        SufixoCartao outroSufixo)
    {
        Fatura sut = Fatura.Criar(
            modelo.Banco,
            modelo.Vencimento,
            [
                LancamentoFatura.Criar(primeiro, sufixo),
                LancamentoFatura.Criar(doOutroCartao, outroSufixo),
                LancamentoFatura.Criar(segundo, sufixo),
            ]).Value;

        sut.LancamentosCartao(sufixo).Should().Equal(primeiro, segundo);
    }

    [Theory, DomainAutoData]
    public void PeriodoCartao_Quando_DatasForaDeOrdem_Deve_IrDaMenorAMaiorDataDeCompraDoCartao(
        Fatura modelo,
        LancamentoCartao lancamento,
        DateOnly dia,
        SufixoCartao sufixo,
        SufixoCartao outroSufixo)
    {
        Fatura sut = Fatura.Criar(
            modelo.Banco,
            modelo.Vencimento,
            [
                LancamentoFatura.Criar(NaData(lancamento, dia), sufixo),
                LancamentoFatura.Criar(NaData(lancamento, dia.AddDays(-270)), sufixo),
                LancamentoFatura.Criar(NaData(lancamento, dia.AddDays(-400)), outroSufixo),
                LancamentoFatura.Criar(NaData(lancamento, dia.AddDays(-10)), sufixo),
                LancamentoFatura.Criar(NaData(lancamento, dia.AddDays(5)), outroSufixo),
            ]).Value;

        sut.PeriodoCartao(sufixo).Should().Be(new Periodo(dia.AddDays(-270), dia));
    }

    [Theory, DomainAutoData]
    public void Periodo_Quando_VariosCartoes_Deve_IrDaMenorAMaiorDataDeCompraDeTodos(
        Fatura modelo,
        LancamentoCartao lancamento,
        DateOnly dia,
        SufixoCartao sufixo,
        SufixoCartao outroSufixo)
    {
        Fatura sut = Fatura.Criar(
            modelo.Banco,
            modelo.Vencimento,
            [
                LancamentoFatura.Criar(NaData(lancamento, dia), sufixo),
                LancamentoFatura.Criar(NaData(lancamento, dia.AddDays(-400)), outroSufixo),
                LancamentoFatura.Criar(NaData(lancamento, dia.AddDays(-10)), sufixo),
                LancamentoFatura.Criar(NaData(lancamento, dia.AddDays(5)), outroSufixo),
            ]).Value;

        sut.Periodo.Should().Be(new Periodo(dia.AddDays(-400), dia.AddDays(5)));
    }

    [Theory, DomainAutoData]
    public void LancamentosCartao_Quando_FinalForaDaFatura_Deve_LancarExcecao(Fatura sut, SufixoCartao foraDaFatura)
    {
        Func<IReadOnlyList<LancamentoCartao>> act = () => sut.LancamentosCartao(foraDaFatura);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("sufixo");
    }

    [Theory, DomainAutoData]
    public void PeriodoCartao_Quando_FinalForaDaFatura_Deve_LancarExcecao(Fatura sut, SufixoCartao foraDaFatura)
    {
        Func<Periodo> act = () => sut.PeriodoCartao(foraDaFatura);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("sufixo");
    }

    [Theory, DomainAutoData]
    public void Equals_Quando_MesmosCampos_Deve_SerIgual(Fatura fatura)
    {
        Fatura outra = Fatura.Criar(fatura.Banco, fatura.Vencimento, fatura.Lancamentos).Value;

        outra.Should().Be(fatura);
    }

    [Theory, DomainAutoData]
    public void Equals_Quando_OutroBanco_Deve_SerDiferente(Fatura fatura)
    {
        Banco outroBanco = Banco.List.First(banco => banco != fatura.Banco);

        Fatura outra = Fatura.Criar(outroBanco, fatura.Vencimento, fatura.Lancamentos).Value;

        outra.Should().NotBe(fatura);
    }

    [Theory, DomainAutoData]
    public void Equals_Quando_OutroVencimento_Deve_SerDiferente(Fatura fatura)
    {
        Fatura outra = Fatura.Criar(fatura.Banco, (VencimentoFatura)fatura.Vencimento.Value.AddDays(1), fatura.Lancamentos).Value;

        outra.Should().NotBe(fatura);
    }

    [Theory, DomainAutoData]
    public void Equals_Quando_LancamentoAMais_Deve_SerDiferente(Fatura fatura, LancamentoFatura aMais)
    {
        Fatura outra = Fatura.Criar(fatura.Banco, fatura.Vencimento, fatura.Lancamentos.Append(aMais)).Value;

        outra.Should().NotBe(fatura);
    }

    [Theory, DomainAutoData]
    public void Equals_Quando_LancamentosEmOutraOrdem_Deve_SerDiferente(
        Fatura modelo,
        LancamentoFatura primeiro,
        LancamentoFatura segundo)
    {
        Fatura fatura = Fatura.Criar(modelo.Banco, modelo.Vencimento, [primeiro, segundo]).Value;

        Fatura outra = Fatura.Criar(modelo.Banco, modelo.Vencimento, [segundo, primeiro]).Value;

        outra.Should().NotBe(fatura);
    }

    [Theory, DomainAutoData]
    public void ConferirPagamentoAnterior_Quando_HaLancamentoDePagamentoFatura_Deve_TerSucesso(
        Fatura modelo,
        LancamentoFatura compra,
        LancamentoCartao lancamento)
    {
        LancamentoFatura pagamento = LancamentoFatura.Criar(lancamento.ComoPagamentoFaturaAnterior(), compra.SufixoCartao);
        Fatura fatura = Fatura.Criar(modelo.Banco, modelo.Vencimento, [compra, pagamento]).Value;

        Result result = fatura.ConferirPagamentoAnterior();

        result.Should().Succeed();
    }

    [Theory, DomainAutoData]
    public void ConferirPagamentoAnterior_Quando_NenhumLancamentoDePagamentoFatura_Deve_Falhar(Fatura fatura)
    {
        Result result = fatura.ConferirPagamentoAnterior();

        result.Should().FailWith("A fatura não traz o pagamento da fatura anterior: se não é a primeira fatura do cartão, confira se o layout do banco mudou.");
    }

    [Theory, DomainAutoData]
    public void ConferirVencimento_Quando_ComprasAntesDoVencimento_Deve_TerSucesso(Fatura modelo, LancamentoCartao lancamento)
    {
        Fatura fatura = ComLancamentos(
            modelo,
            lancamento.NaFatura(modelo.Vencimento.Value.AddDays(-1), parcela: null).ComoSaida(),
            lancamento.NaFatura(modelo.Vencimento.Value.AddMonths(-2), parcela: null).ComoSaida(),
            lancamento.NaFatura(modelo.Vencimento.Value.AddMonths(-9), (Parcela)(9, 10)).ComoSaida());

        UnitResult<Erro> result = fatura.ConferirVencimento();

        result.Should().Succeed();
    }

    [Theory, DomainAutoData]
    public void ConferirVencimento_Quando_CompraAVistaNoDiaDoVencimento_Deve_Falhar(Fatura modelo, LancamentoCartao lancamento)
    {
        Fatura fatura = ComLancamentos(modelo, lancamento.NaFatura(modelo.Vencimento, parcela: null).ComoSaida());

        UnitResult<Erro> result = fatura.ConferirVencimento();

        result.Should().FailWith(ErroVencimentoIncompativelComFatura.Instancia);
    }

    [Theory]
    [InlineDomainAutoData(0)]
    [InlineDomainAutoData(2)]
    public void ConferirVencimento_Quando_ParcelaAteDoisMesesDepoisDoMesDela_Deve_TerSucesso(
        int mesesDepoisPrimeiroMesPossivel,
        Fatura modelo,
        LancamentoCartao lancamento)
    {
        Parcela parcela = (Parcela)(3, 10);
        DateOnly noPrimeiroMesPossivel = modelo.Vencimento.Value.AddMonths(-mesesDepoisPrimeiroMesPossivel);
        DateOnly dataCompra = noPrimeiroMesPossivel.AddMonths(-parcela.QuantidadeParcelasAnteriores);
        Fatura fatura = ComLancamentos(modelo, lancamento.NaFatura(dataCompra, parcela).ComoSaida());

        UnitResult<Erro> result = fatura.ConferirVencimento();

        result.Should().Succeed();
    }

    [Theory]
    [InlineDomainAutoData(-1)]
    [InlineDomainAutoData(3)]
    public void ConferirVencimento_Quando_ParcelaAntesDoMesDelaOuMaisDeDoisMesesDepois_Deve_Falhar(
        int mesesDepoisPrimeiroMesPossivel,
        Fatura modelo,
        LancamentoCartao lancamento)
    {
        Parcela parcela = (Parcela)(3, 10);
        DateOnly noPrimeiroMesPossivel = modelo.Vencimento.Value.AddMonths(-mesesDepoisPrimeiroMesPossivel);
        DateOnly dataCompra = noPrimeiroMesPossivel.AddMonths(-parcela.QuantidadeParcelasAnteriores);
        Fatura fatura = ComLancamentos(modelo, lancamento.NaFatura(dataCompra, parcela).ComoSaida());

        UnitResult<Erro> result = fatura.ConferirVencimento();

        result.Should().FailWith(ErroVencimentoIncompativelComFatura.Instancia);
    }

    [Theory, DomainAutoData]
    public void ConferirVencimento_Quando_CompraAVistaMaisDeDoisMesesAntes_Deve_Falhar(Fatura modelo, LancamentoCartao lancamento)
    {
        Fatura fatura = ComLancamentos(modelo, lancamento.NaFatura(modelo.Vencimento.Value.AddMonths(-3), parcela: null).ComoSaida());

        UnitResult<Erro> result = fatura.ConferirVencimento();

        result.Should().FailWith(ErroVencimentoIncompativelComFatura.Instancia);
    }

    [Theory, DomainAutoData]
    public void ConferirVencimento_Quando_PagamentoEEstornoDepoisDoVencimento_Deve_TerSucesso(
        Fatura modelo,
        LancamentoCartao lancamento)
    {
        Fatura fatura = ComLancamentos(
            modelo,
            lancamento.NaFatura(modelo.Vencimento.Value.AddMonths(1), parcela: null).ComoPagamentoFaturaAnterior(),
            lancamento.NaFatura(modelo.Vencimento.Value.AddMonths(4), parcela: null).ComoEntrada());

        UnitResult<Erro> result = fatura.ConferirVencimento();

        result.Should().Succeed();
    }

    /// <summary>A fatura do mesmo banco e com o mesmo vencimento do modelo, só com estes lançamentos, num final dela.</summary>
    private static Fatura ComLancamentos(Fatura modelo, params LancamentoCartao[] lancamentos)
    {
        return Fatura
            .Criar(modelo.Banco, modelo.Vencimento, lancamentos.Select(lancamento => LancamentoFatura.Criar(lancamento, modelo.Sufixos[0])))
            .Value;
    }

    private static LancamentoCartao NaData(LancamentoCartao lancamento, DateOnly data)
    {
        return LancamentoCartao.Criar(
            data,
            lancamento.Hora,
            lancamento.Valor,
            lancamento.Descricao,
            lancamento.Contraparte,
            lancamento.Parcela,
            lancamento.CategoriaBanco,
            lancamento.Identidade);
    }
}
