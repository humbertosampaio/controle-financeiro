using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;
using System.Globalization;

namespace ControleFinanceiro.Domain.Tests.Importacoes;

public sealed class ConferenciaSaldoTests
{
    [Theory, DomainAutoData]
    public void Conferir_Quando_SaldosFecham_Deve_RetornarSucesso(
        Extrato arquivo,
        DateOnly dia,
        Reais saldoBase,
        Lancamento primeiro,
        Lancamento segundo,
        Lancamento terceiro)
    {
        DateOnly diaSeguinte = dia.AddDays(3);
        Extrato extrato = Montar(
            arquivo,
            Periodo(dia, diaSeguinte),
            [LancamentoContabilizado.Criar(primeiro, dia), LancamentoContabilizado.Criar(segundo, diaSeguinte), LancamentoContabilizado.Criar(terceiro, diaSeguinte)],
            [SaldoDiario.Criar(dia, saldoBase), SaldoDiario.Criar(diaSeguinte, saldoBase + segundo.Valor + terceiro.Valor)]);

        Result result = ConferenciaSaldo.Conferir(extrato);

        result.Should().Succeed();
    }

    [Theory, DomainAutoData]
    public void Conferir_Quando_SaldoDeUmDiaNaoFecha_Deve_RetornarFalhaComAData(
        Extrato arquivo,
        DateOnly dia,
        Reais saldoBase,
        Lancamento primeiro,
        Lancamento segundo)
    {
        DateOnly diaSeguinte = dia.AddDays(1);
        Extrato extrato = Montar(
            arquivo,
            Periodo(dia, diaSeguinte),
            [LancamentoContabilizado.Criar(primeiro, dia), LancamentoContabilizado.Criar(segundo, diaSeguinte)],
            [SaldoDiario.Criar(dia, saldoBase), SaldoDiario.Criar(diaSeguinte, saldoBase + segundo.Valor + 0.01m)]);

        Result result = ConferenciaSaldo.Conferir(extrato);

        result.Should().FailWith($"O saldo do dia {Formatar(diaSeguinte)} não fecha com os lançamentos do extrato.");
    }

    [Theory, DomainAutoData]
    public void Conferir_Quando_LancamentoComDataContabilNoDiaSeguinte_Deve_FecharOSaldoPelaDataContabil(
        Extrato arquivo,
        DateOnly sabado,
        Reais saldoBase,
        Lancamento anterior,
        Lancamento pixDeSabado)
    {
        DateOnly sexta = sabado.AddDays(-1);
        DateOnly segunda = sabado.AddDays(2);
        Lancamento pix = Lancamento.Criar(
            sabado, pixDeSabado.Hora, pixDeSabado.Valor, pixDeSabado.Descricao, pixDeSabado.Contraparte, pixDeSabado.Identidade);
        Extrato extrato = Montar(
            arquivo,
            Periodo(sexta, segunda),
            [LancamentoContabilizado.Criar(anterior, sexta), LancamentoContabilizado.Criar(pix, segunda)],
            [SaldoDiario.Criar(sexta, saldoBase), SaldoDiario.Criar(segunda, saldoBase + pix.Valor)]);

        Result result = ConferenciaSaldo.Conferir(extrato);

        result.Should().Succeed();
    }

    [Theory, DomainAutoData]
    public void Conferir_Quando_UmDiaContabilSo_Deve_RetornarSucesso(
        Extrato arquivo,
        DateOnly dia,
        Reais saldo,
        Lancamento lancamento)
    {
        Extrato extrato = Montar(
            arquivo,
            Periodo(dia, dia),
            [LancamentoContabilizado.Criar(lancamento, dia)],
            [SaldoDiario.Criar(dia, saldo)]);

        Result result = ConferenciaSaldo.Conferir(extrato);

        result.Should().Succeed();
    }

    [Theory, DomainAutoData]
    public void Conferir_Quando_DiaContabilDepoisDoFimNaoFecha_Deve_IgnorarODia(
        Extrato arquivo,
        DateOnly fim,
        Reais saldoBase,
        Lancamento noFim,
        Lancamento depoisDoFim)
    {
        DateOnly diaUtilSeguinte = fim.AddDays(2);
        Extrato extrato = Montar(
            arquivo,
            Periodo(fim, fim),
            [LancamentoContabilizado.Criar(noFim, fim), LancamentoContabilizado.Criar(depoisDoFim, diaUtilSeguinte)],
            [SaldoDiario.Criar(fim, saldoBase), SaldoDiario.Criar(diaUtilSeguinte, saldoBase + depoisDoFim.Valor + 0.01m)]);

        Result result = ConferenciaSaldo.Conferir(extrato);

        result.Should().Succeed();
    }

    [Theory, DomainAutoData]
    public void Conferir_Quando_DiaSemSaldo_Deve_RetornarFalhaComAData(
        Extrato arquivo,
        DateOnly dia,
        Reais saldoBase,
        Lancamento primeiro,
        Lancamento segundo)
    {
        DateOnly diaSeguinte = dia.AddDays(1);
        Extrato extrato = Montar(
            arquivo,
            Periodo(dia, diaSeguinte),
            [LancamentoContabilizado.Criar(primeiro, dia), LancamentoContabilizado.Criar(segundo, diaSeguinte)],
            [SaldoDiario.Criar(dia, saldoBase)]);

        Result result = ConferenciaSaldo.Conferir(extrato);

        result.Should().FailWith($"O extrato não traz o saldo do dia {Formatar(diaSeguinte)}.");
    }

    [Theory, DomainAutoData]
    public void Conferir_Quando_PrimeiroDiaSemSaldo_Deve_RetornarFalhaComAData(
        Extrato arquivo,
        DateOnly dia,
        Reais saldo,
        Lancamento primeiro,
        Lancamento segundo)
    {
        DateOnly diaSeguinte = dia.AddDays(1);
        Extrato extrato = Montar(
            arquivo,
            Periodo(dia, diaSeguinte),
            [LancamentoContabilizado.Criar(primeiro, dia), LancamentoContabilizado.Criar(segundo, diaSeguinte)],
            [SaldoDiario.Criar(diaSeguinte, saldo)]);

        Result result = ConferenciaSaldo.Conferir(extrato);

        result.Should().FailWith($"O extrato não traz o saldo do dia {Formatar(dia)}.");
    }

    [Theory, DomainAutoData]
    public void Conferir_Quando_DiasComMaisDeUmSaldo_Deve_RetornarFalhaComTodasAsDatas(
        Extrato arquivo,
        DateOnly dia,
        Reais saldo,
        Reais outroSaldo,
        Lancamento lancamento)
    {
        DateOnly diaSemRepeticao = dia.AddDays(1);
        DateOnly outroDia = dia.AddDays(2);
        Extrato extrato = Montar(
            arquivo,
            Periodo(dia, outroDia),
            [LancamentoContabilizado.Criar(lancamento, dia)],
            [
                SaldoDiario.Criar(outroDia, saldo),
                SaldoDiario.Criar(dia, saldo),
                SaldoDiario.Criar(diaSemRepeticao, saldo),
                SaldoDiario.Criar(outroDia, outroSaldo),
                SaldoDiario.Criar(dia, outroSaldo),
            ]);

        Result result = ConferenciaSaldo.Conferir(extrato);

        result.Should().FailWith($"O extrato traz mais de um saldo nestes dias: {Formatar(dia)}, {Formatar(outroDia)}.");
    }

    private static Extrato Montar(
        Extrato arquivo,
        PeriodoExtrato periodo,
        IEnumerable<LancamentoContabilizado> lancamentos,
        IEnumerable<SaldoDiario> saldos)
    {
        return Extrato.Criar(arquivo.Banco, arquivo.TipoConta, arquivo.SufixoConta, periodo, lancamentos, saldos);
    }

    private static PeriodoExtrato Periodo(DateOnly inicio, DateOnly fim) => PeriodoExtrato.Criar(inicio, fim).Value;

    private static string Formatar(DateOnly data) => data.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
}
