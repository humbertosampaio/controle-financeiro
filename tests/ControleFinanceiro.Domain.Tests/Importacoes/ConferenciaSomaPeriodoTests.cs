using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;
using System.Globalization;

namespace ControleFinanceiro.Domain.Tests.Importacoes;

public sealed class ConferenciaSomaPeriodoTests
{
    [Theory, LancamentosNoPeriodoAutoData]
    public void Conferir_Quando_PrimeiraImportacao_Deve_RetornarSucesso(
        ExtratoLido extrato,
        [Frozen] ContaBancaria conta,
        ImportacaoExtrato importacao)
    {
        Result result = ConferenciaSomaPeriodo.Conferir(extrato, gravadasNoPeriodo: [], Lote(conta, importacao, extrato.Lancamentos));

        result.Should().Succeed();
    }

    [Theory, LancamentosNoPeriodoAutoData]
    public void Conferir_Quando_ParteDoPeriodoJaGravada_Deve_SomarAsGravadasEAsNovas(
        ExtratoLido extrato,
        [Frozen] ContaBancaria conta,
        ImportacaoExtrato importacao)
    {
        IReadOnlyList<Transacao> lote = Lote(conta, importacao, extrato.Lancamentos);

        Result result = ConferenciaSomaPeriodo.Conferir(extrato, lote.Take(1), lote.Skip(1));

        result.Should().Succeed();
    }

    [Theory, LancamentosNoPeriodoAutoData]
    public void Conferir_Quando_GravadaQueOExtratoNaoTraz_Deve_RetornarFalhaComOPeriodo(
        [Frozen] PeriodoExtrato periodo,
        ExtratoLido extrato,
        [Frozen] ContaBancaria conta,
        ImportacaoExtrato importacao,
        LancamentoExtrato naoReconhecido)
    {
        IReadOnlyList<Transacao> gravadas = Lote(conta, importacao, [.. extrato.Lancamentos, naoReconhecido]);

        Result result = ConferenciaSomaPeriodo.Conferir(extrato, gravadas, novas: []);

        result.Should().FailWith(
            $"A soma das transações gravadas de {Formatar(periodo.Inicio)} a {Formatar(periodo.Fim)} não fecha com a soma dos lançamentos do extrato.");
    }

    [Theory, LancamentosNoPeriodoAutoData]
    public void Conferir_Quando_ExtratoTrazLancamentoForaDoPeriodo_Deve_RetornarFalha(
        [Frozen] PeriodoExtrato periodo,
        ExtratoLido arquivo,
        [Frozen] ContaBancaria conta,
        ImportacaoExtrato importacao,
        Reais valor,
        TextoObrigatorio descricao)
    {
        DateOnly depoisDoFim = periodo.Fim.AddDays(1);
        LancamentoExtrato foraDoPeriodo = LancamentoExtrato.Criar(
            depoisDoFim, hora: null, valor, descricao, contraparte: null, IdentidadeLancamento.Composta(depoisDoFim).ComValor(valor).ComTexto(descricao));
        ExtratoLido extrato = ExtratoLido.Criar(
            arquivo.Banco,
            arquivo.TipoConta,
            arquivo.SufixoConta,
            periodo,
            [.. arquivo.LancamentosLidos, LancamentoLido.Criar(foraDoPeriodo, depoisDoFim)],
            saldos: []);

        Result result = ConferenciaSomaPeriodo.Conferir(extrato, gravadasNoPeriodo: [], Lote(conta, importacao, extrato.Lancamentos));

        result.Should().Fail();
    }

    private static IReadOnlyList<Transacao> Lote(ContaBancaria conta, ImportacaoExtrato importacao, IReadOnlyList<LancamentoExtrato> lancamentos)
    {
        return LoteTransacoesFactory.Criar(conta.IdUsuario, conta, importacao, lancamentos).Value;
    }

    private static string Formatar(DateOnly data) => data.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    /// <summary>
    /// O extrato traz três lançamentos no primeiro dia do próprio período, e todo lançamento pedido à parte cai
    /// no primeiro dia de um período. Com <c>[Frozen] PeriodoExtrato</c> no teste, os dois ficam no mesmo período.
    /// </summary>
    private sealed class LancamentosNoPeriodoAutoDataAttribute : DomainAutoDataAttribute
    {
        public LancamentosNoPeriodoAutoDataAttribute()
            : base(fixture =>
            {
                fixture.Register((PeriodoExtrato periodo, Reais valor, TextoObrigatorio descricao) => NoInicio(periodo, valor, descricao));

                fixture.Register((Banco banco, TipoConta tipo, SufixoConta sufixo, PeriodoExtrato periodo) => ExtratoLido.Criar(
                    banco,
                    tipo,
                    sufixo,
                    periodo,
                    Enumerable.Range(start: 0, count: 3)
                        .Select(_ => NoInicio(periodo, fixture.Create<Reais>(), fixture.Create<TextoObrigatorio>()))
                        .Select(l => LancamentoLido.Criar(l, l.Data)),
                    saldos: []));
            })
        {
        }

        private static LancamentoExtrato NoInicio(PeriodoExtrato periodo, Reais valor, TextoObrigatorio descricao)
        {
            IdentidadeLancamento identidade = IdentidadeLancamento.Composta(periodo.Inicio).ComValor(valor).ComTexto(descricao);
            return LancamentoExtrato.Criar(periodo.Inicio, hora: null, valor, descricao, contraparte: null, identidade);
        }
    }
}
