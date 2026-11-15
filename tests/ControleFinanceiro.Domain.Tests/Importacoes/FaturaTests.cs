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
    public void Criar_Quando_SemLancamentos_Deve_RetornarFalha(Banco banco, DateOnly vencimento)
    {
        Result<Fatura> result = Fatura.Criar(banco, vencimento, lancamentos: []);

        result.Should().FailWith("A fatura não traz lançamentos.");
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_GuardarOBancoOVencimentoEOsLancamentosNaOrdemDoArquivo(
        Banco banco,
        DateOnly vencimento,
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
        Banco banco,
        DateOnly vencimento,
        Lancamento lancamento,
        SufixoCartao fisico,
        SufixoCartao virtualCartao)
    {
        Fatura sut = Fatura.Criar(
                banco,
                vencimento,
                [
                    LancamentoFatura.Criar(lancamento, virtualCartao),
                    LancamentoFatura.Criar(lancamento, fisico),
                    LancamentoFatura.Criar(lancamento, virtualCartao),
                ])
            .Value;

        sut.Sufixos.Should().Equal(virtualCartao, fisico);
    }

    [Theory, DomainAutoData]
    public void LancamentosCartao_Deve_TrazerSoOsDoFinalNaOrdemDoArquivo(
        Banco banco,
        DateOnly vencimento,
        Lancamento primeiro,
        Lancamento doOutroCartao,
        Lancamento segundo,
        SufixoCartao sufixo,
        SufixoCartao outroSufixo)
    {
        Fatura sut = Fatura.Criar(
                banco,
                vencimento,
                [
                    LancamentoFatura.Criar(primeiro, sufixo),
                    LancamentoFatura.Criar(doOutroCartao, outroSufixo),
                    LancamentoFatura.Criar(segundo, sufixo),
                ])
            .Value;

        sut.LancamentosCartao(sufixo).Should().Equal(primeiro, segundo);
    }

    [Theory, DomainAutoData]
    public void PeriodoCartao_Quando_DatasForaDeOrdem_Deve_IrDaMenorAMaiorDataDeCompraDoCartao(
        Banco banco,
        DateOnly vencimento,
        Lancamento lancamento,
        DateOnly dia,
        SufixoCartao sufixo,
        SufixoCartao outroSufixo)
    {
        Fatura sut = Fatura.Criar(
                banco,
                vencimento,
                [
                    LancamentoFatura.Criar(NaData(lancamento, dia), sufixo),
                    LancamentoFatura.Criar(NaData(lancamento, dia.AddDays(-270)), sufixo),
                    LancamentoFatura.Criar(NaData(lancamento, dia.AddDays(-400)), outroSufixo),
                    LancamentoFatura.Criar(NaData(lancamento, dia.AddDays(-10)), sufixo),
                    LancamentoFatura.Criar(NaData(lancamento, dia.AddDays(5)), outroSufixo),
                ])
            .Value;

        sut.PeriodoCartao(sufixo).Should().Be((PeriodoArquivo)(dia.AddDays(-270), dia));
    }

    [Theory, DomainAutoData]
    public void Periodo_Quando_VariosCartoes_Deve_IrDaMenorAMaiorDataDeCompraDeTodos(
        Banco banco,
        DateOnly vencimento,
        Lancamento lancamento,
        DateOnly dia,
        SufixoCartao sufixo,
        SufixoCartao outroSufixo)
    {
        Fatura sut = Fatura.Criar(
                banco,
                vencimento,
                [
                    LancamentoFatura.Criar(NaData(lancamento, dia), sufixo),
                    LancamentoFatura.Criar(NaData(lancamento, dia.AddDays(-400)), outroSufixo),
                    LancamentoFatura.Criar(NaData(lancamento, dia.AddDays(-10)), sufixo),
                    LancamentoFatura.Criar(NaData(lancamento, dia.AddDays(5)), outroSufixo),
                ])
            .Value;

        sut.Periodo.Should().Be((PeriodoArquivo)(dia.AddDays(-400), dia.AddDays(5)));
    }

    [Theory, DomainAutoData]
    public void LancamentosCartao_Quando_FinalForaDaFatura_Deve_LancarExcecao(Fatura sut, SufixoCartao foraDaFatura)
    {
        Func<IReadOnlyList<Lancamento>> act = () => sut.LancamentosCartao(foraDaFatura);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("sufixo");
    }

    [Theory, DomainAutoData]
    public void PeriodoCartao_Quando_FinalForaDaFatura_Deve_LancarExcecao(Fatura sut, SufixoCartao foraDaFatura)
    {
        Func<PeriodoArquivo> act = () => sut.PeriodoCartao(foraDaFatura);

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
        Fatura outra = Fatura.Criar(fatura.Banco, fatura.Vencimento.AddDays(1), fatura.Lancamentos).Value;

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
        Banco banco,
        DateOnly vencimento,
        LancamentoFatura primeiro,
        LancamentoFatura segundo)
    {
        Fatura fatura = Fatura.Criar(banco, vencimento, [primeiro, segundo]).Value;

        Fatura outra = Fatura.Criar(banco, vencimento, [segundo, primeiro]).Value;

        outra.Should().NotBe(fatura);
    }

    [Theory, DomainAutoData]
    public void ConferirPagamentoAnterior_Quando_HaLancamentoDePagamentoFatura_Deve_TerSucesso(
        Banco banco,
        DateOnly vencimento,
        LancamentoFatura compra,
        Lancamento lancamento)
    {
        LancamentoFatura pagamento = LancamentoFatura.Criar(lancamento.ComoPagamentoFatura(), compra.SufixoCartao);
        Fatura fatura = Fatura.Criar(banco, vencimento, [compra, pagamento]).Value;

        Result result = fatura.ConferirPagamentoAnterior();

        result.Should().Succeed();
    }

    [Theory, DomainAutoData]
    public void ConferirPagamentoAnterior_Quando_NenhumLancamentoDePagamentoFatura_Deve_Falhar(Fatura fatura)
    {
        Result result = fatura.ConferirPagamentoAnterior();

        result.Should().FailWith(Fatura.AvisoSemPagamentoAnterior);
    }

    [Theory, DomainAutoData]
    public void ConferirVencimento_Quando_ComprasAntesDoVencimento_Deve_TerSucesso(Fatura modelo, Lancamento lancamento)
    {
        Fatura fatura = ComLancamentos(
            modelo,
            lancamento.NaFatura(modelo.Vencimento.AddDays(-1), parcela: null).ComoSaida(),
            lancamento.NaFatura(modelo.Vencimento.AddMonths(-2), parcela: null).ComoSaida(),
            lancamento.NaFatura(modelo.Vencimento.AddMonths(-9), (Parcela)(9, 10)).ComoSaida());

        UnitResult<Erro> result = fatura.ConferirVencimento();

        result.Should().Succeed();
    }

    [Theory, DomainAutoData]
    public void ConferirVencimento_Quando_CompraAVistaNoDiaDoVencimento_Deve_Falhar(Fatura modelo, Lancamento lancamento)
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
        Lancamento lancamento)
    {
        Parcela parcela = (Parcela)(3, 10);
        DateOnly noPrimeiroMesPossivel = modelo.Vencimento.AddMonths(-mesesDepoisPrimeiroMesPossivel);
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
        Lancamento lancamento)
    {
        Parcela parcela = (Parcela)(3, 10);
        DateOnly noPrimeiroMesPossivel = modelo.Vencimento.AddMonths(-mesesDepoisPrimeiroMesPossivel);
        DateOnly dataCompra = noPrimeiroMesPossivel.AddMonths(-parcela.QuantidadeParcelasAnteriores);
        Fatura fatura = ComLancamentos(modelo, lancamento.NaFatura(dataCompra, parcela).ComoSaida());

        UnitResult<Erro> result = fatura.ConferirVencimento();

        result.Should().FailWith(ErroVencimentoIncompativelComFatura.Instancia);
    }

    [Theory, DomainAutoData]
    public void ConferirVencimento_Quando_CompraAVistaMaisDeDoisMesesAntes_Deve_Falhar(Fatura modelo, Lancamento lancamento)
    {
        Fatura fatura = ComLancamentos(modelo, lancamento.NaFatura(modelo.Vencimento.AddMonths(-3), parcela: null).ComoSaida());

        UnitResult<Erro> result = fatura.ConferirVencimento();

        result.Should().FailWith(ErroVencimentoIncompativelComFatura.Instancia);
    }

    [Theory, DomainAutoData]
    public void ConferirVencimento_Quando_PagamentoEEstornoDepoisDoVencimento_Deve_TerSucesso(
        Fatura modelo,
        Lancamento lancamento)
    {
        Fatura fatura = ComLancamentos(
            modelo,
            lancamento.NaFatura(modelo.Vencimento.AddMonths(1), parcela: null).ComoPagamentoFatura(),
            lancamento.NaFatura(modelo.Vencimento.AddMonths(4), parcela: null).ComoEntrada());

        UnitResult<Erro> result = fatura.ConferirVencimento();

        result.Should().Succeed();
    }

    /// <summary>A fatura do mesmo banco e com o mesmo vencimento do modelo, só com estes lançamentos, num final dela.</summary>
    private static Fatura ComLancamentos(Fatura modelo, params Lancamento[] lancamentos)
    {
        return Fatura
            .Criar(modelo.Banco, modelo.Vencimento, lancamentos.Select(lancamento => LancamentoFatura.Criar(lancamento, modelo.Sufixos[0])))
            .Value;
    }

    private static Lancamento NaData(Lancamento lancamento, DateOnly data)
    {
        return Lancamento.DoCartao(
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
