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
        ExtratoLido arquivo,
        DateOnly dia,
        Reais saldoBase,
        LancamentoExtrato primeiro,
        LancamentoExtrato segundo,
        LancamentoExtrato terceiro)
    {
        DateOnly diaSeguinte = dia.AddDays(3);
        ExtratoLido extrato = Extrato(
            arquivo,
            Periodo(dia, diaSeguinte),
            [LancamentoLido.Criar(primeiro, dia), LancamentoLido.Criar(segundo, diaSeguinte), LancamentoLido.Criar(terceiro, diaSeguinte)],
            [SaldoDiario.Criar(dia, saldoBase), SaldoDiario.Criar(diaSeguinte, saldoBase + segundo.Valor + terceiro.Valor)]);

        Result result = ConferenciaSaldo.Conferir(extrato);

        result.Should().Succeed();
    }

    [Theory, DomainAutoData]
    public void Conferir_Quando_SaldoDeUmDiaNaoFecha_Deve_RetornarFalhaComAData(
        ExtratoLido arquivo,
        DateOnly dia,
        Reais saldoBase,
        LancamentoExtrato primeiro,
        LancamentoExtrato segundo)
    {
        DateOnly diaSeguinte = dia.AddDays(1);
        ExtratoLido extrato = Extrato(
            arquivo,
            Periodo(dia, diaSeguinte),
            [LancamentoLido.Criar(primeiro, dia), LancamentoLido.Criar(segundo, diaSeguinte)],
            [SaldoDiario.Criar(dia, saldoBase), SaldoDiario.Criar(diaSeguinte, saldoBase + segundo.Valor + 0.01m)]);

        Result result = ConferenciaSaldo.Conferir(extrato);

        result.Should().FailWith($"O saldo do dia {Formatar(diaSeguinte)} não fecha com os lançamentos do extrato.");
    }

    [Theory, DomainAutoData]
    public void Conferir_Quando_LancamentoComDataContabilNoDiaSeguinte_Deve_FecharOSaldoPelaDataContabil(
        ExtratoLido arquivo,
        DateOnly sabado,
        Reais saldoBase,
        LancamentoExtrato anterior,
        LancamentoExtrato pixDeSabado)
    {
        DateOnly sexta = sabado.AddDays(-1);
        DateOnly segunda = sabado.AddDays(2);
        LancamentoExtrato pix = LancamentoExtrato.Criar(
            sabado, pixDeSabado.Hora, pixDeSabado.Valor, pixDeSabado.Descricao, pixDeSabado.Contraparte, pixDeSabado.Identidade);
        ExtratoLido extrato = Extrato(
            arquivo,
            Periodo(sexta, segunda),
            [LancamentoLido.Criar(anterior, sexta), LancamentoLido.Criar(pix, segunda)],
            [SaldoDiario.Criar(sexta, saldoBase), SaldoDiario.Criar(segunda, saldoBase + pix.Valor)]);

        Result result = ConferenciaSaldo.Conferir(extrato);

        result.Should().Succeed();
    }

    [Theory, DomainAutoData]
    public void Conferir_Quando_UmDiaContabilSo_Deve_RetornarSucesso(
        ExtratoLido arquivo,
        DateOnly dia,
        Reais saldo,
        LancamentoExtrato lancamento)
    {
        ExtratoLido extrato = Extrato(
            arquivo,
            Periodo(dia, dia),
            [LancamentoLido.Criar(lancamento, dia)],
            [SaldoDiario.Criar(dia, saldo)]);

        Result result = ConferenciaSaldo.Conferir(extrato);

        result.Should().Succeed();
    }

    [Theory, DomainAutoData]
    public void Conferir_Quando_DiaContabilDepoisDoFimNaoFecha_Deve_IgnorarODia(
        ExtratoLido arquivo,
        DateOnly fim,
        Reais saldoBase,
        LancamentoExtrato noFim,
        LancamentoExtrato depoisDoFim)
    {
        DateOnly diaUtilSeguinte = fim.AddDays(2);
        ExtratoLido extrato = Extrato(
            arquivo,
            Periodo(fim, fim),
            [LancamentoLido.Criar(noFim, fim), LancamentoLido.Criar(depoisDoFim, diaUtilSeguinte)],
            [SaldoDiario.Criar(fim, saldoBase), SaldoDiario.Criar(diaUtilSeguinte, saldoBase + depoisDoFim.Valor + 0.01m)]);

        Result result = ConferenciaSaldo.Conferir(extrato);

        result.Should().Succeed();
    }

    [Theory, DomainAutoData]
    public void Conferir_Quando_DiaSemSaldo_Deve_RetornarFalhaComAData(
        ExtratoLido arquivo,
        DateOnly dia,
        Reais saldoBase,
        LancamentoExtrato primeiro,
        LancamentoExtrato segundo)
    {
        DateOnly diaSeguinte = dia.AddDays(1);
        ExtratoLido extrato = Extrato(
            arquivo,
            Periodo(dia, diaSeguinte),
            [LancamentoLido.Criar(primeiro, dia), LancamentoLido.Criar(segundo, diaSeguinte)],
            [SaldoDiario.Criar(dia, saldoBase)]);

        Result result = ConferenciaSaldo.Conferir(extrato);

        result.Should().FailWith($"O extrato não traz o saldo do dia {Formatar(diaSeguinte)}.");
    }

    [Theory, DomainAutoData]
    public void Conferir_Quando_PrimeiroDiaSemSaldo_Deve_RetornarFalhaComAData(
        ExtratoLido arquivo,
        DateOnly dia,
        Reais saldo,
        LancamentoExtrato primeiro,
        LancamentoExtrato segundo)
    {
        DateOnly diaSeguinte = dia.AddDays(1);
        ExtratoLido extrato = Extrato(
            arquivo,
            Periodo(dia, diaSeguinte),
            [LancamentoLido.Criar(primeiro, dia), LancamentoLido.Criar(segundo, diaSeguinte)],
            [SaldoDiario.Criar(diaSeguinte, saldo)]);

        Result result = ConferenciaSaldo.Conferir(extrato);

        result.Should().FailWith($"O extrato não traz o saldo do dia {Formatar(dia)}.");
    }

    [Theory, DomainAutoData]
    public void Conferir_Quando_DiasComMaisDeUmSaldo_Deve_RetornarFalhaComTodasAsDatas(
        ExtratoLido arquivo,
        DateOnly dia,
        Reais saldo,
        Reais outroSaldo,
        LancamentoExtrato lancamento)
    {
        DateOnly diaSemRepeticao = dia.AddDays(1);
        DateOnly outroDia = dia.AddDays(2);
        ExtratoLido extrato = Extrato(
            arquivo,
            Periodo(dia, outroDia),
            [LancamentoLido.Criar(lancamento, dia)],
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

    private static ExtratoLido Extrato(
        ExtratoLido arquivo,
        PeriodoExtrato periodo,
        IEnumerable<LancamentoLido> lancamentos,
        IEnumerable<SaldoDiario> saldos)
    {
        return ExtratoLido.Criar(arquivo.Banco, arquivo.TipoConta, arquivo.SufixoConta, periodo, lancamentos, saldos);
    }

    private static PeriodoExtrato Periodo(DateOnly inicio, DateOnly fim) => PeriodoExtrato.Criar(inicio, fim).Value;

    private static string Formatar(DateOnly data) => data.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
}
