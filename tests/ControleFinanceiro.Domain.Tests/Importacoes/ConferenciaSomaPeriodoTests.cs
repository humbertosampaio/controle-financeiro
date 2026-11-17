using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Tests.Services;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;
using System.Globalization;

namespace ControleFinanceiro.Domain.Tests.Importacoes;

public sealed class ConferenciaSomaPeriodoTests
{
    [Theory, LancamentosNoPeriodoAutoData]
    public void Conferir_Quando_PrimeiraImportacao_Deve_RetornarSucesso(
        Extrato extrato,
        [Frozen] ContaBancaria conta,
        ImportacaoExtrato importacao)
    {
        Result result = ConferenciaSomaPeriodo.Conferir(extrato, gravadasNoPeriodo: [], Lote(conta, importacao, extrato.Lancamentos));

        result.Should().Succeed();
    }

    [Theory, LancamentosNoPeriodoAutoData]
    public void Conferir_Quando_ParteDoPeriodoJaGravada_Deve_SomarAsGravadasEAsNovas(
        Extrato extrato,
        [Frozen] ContaBancaria conta,
        ImportacaoExtrato importacao)
    {
        Transacao[] lote = Lote(conta, importacao, extrato.Lancamentos);

        Result result = ConferenciaSomaPeriodo.Conferir(extrato, lote.Take(1), lote.Skip(1));

        result.Should().Succeed();
    }

    [Theory, LancamentosNoPeriodoAutoData]
    public void Conferir_Quando_GravadaQueOExtratoNaoTraz_Deve_RetornarFalhaComOPeriodo(
        [Frozen] Periodo periodo,
        Extrato extrato,
        [Frozen] ContaBancaria conta,
        ImportacaoExtrato importacao,
        LancamentoConta naoReconhecido)
    {
        Transacao[] gravadas = Lote(conta, importacao, [.. extrato.Lancamentos, naoReconhecido]);

        Result result = ConferenciaSomaPeriodo.Conferir(extrato, gravadas, novas: []);

        result.Should().FailWith(
            $"A soma das transações gravadas de {Formatar(periodo.Inicio)} a {Formatar(periodo.Fim)} não fecha com a soma dos lançamentos do extrato.");
    }

    [Theory, LancamentosNoPeriodoAutoData]
    public void Conferir_Quando_ExtratoTrazLancamentoForaDoPeriodo_Deve_RetornarFalha(
        [Frozen] Periodo periodo,
        Extrato arquivo,
        [Frozen] ContaBancaria conta,
        ImportacaoExtrato importacao,
        Reais valor,
        TextoObrigatorio descricao)
    {
        DateOnly depoisDoFim = periodo.Fim.AddDays(1);
        LancamentoConta foraDoPeriodo = LancamentoConta.Criar(
            depoisDoFim, hora: null, valor, descricao, contraparte: null, categoriaBanco: null, IdentidadeLancamento.Composta(depoisDoFim).ComValor(valor).ComTexto(descricao));
        Extrato extrato = Extrato.Criar(
            arquivo.Banco,
            arquivo.TipoConta,
            arquivo.SufixoConta,
            periodo,
            [.. arquivo.LancamentosContabilizados, LancamentoContabilizado.Criar(foraDoPeriodo, depoisDoFim)],
            saldos: []).Value;

        Result result = ConferenciaSomaPeriodo.Conferir(extrato, gravadasNoPeriodo: [], Lote(conta, importacao, extrato.Lancamentos));

        result.Should().Fail();
    }

    private static Transacao[] Lote(ContaBancaria conta, ImportacaoExtrato importacao, IReadOnlyList<LancamentoConta> lancamentos)
    {
        IReadOnlyList<Beneficiario> beneficiarios = BeneficiariosResolvidos.Dos(conta.IdUsuario, lancamentos);
        IReadOnlyList<CategoriaDespesa> categorias = CategoriasResolvidas.Dos(conta.IdUsuario, lancamentos);
        return LoteTransacoesContaFactory.Criar(conta.IdUsuario, beneficiarios, categorias, conta, importacao, lancamentos).Value.ToArray();
    }

    private static string Formatar(DateOnly data) => data.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    /// <summary>
    /// O extrato traz três lançamentos no primeiro dia do próprio período, e todo lançamento pedido à parte cai
    /// no primeiro dia de um período. Com <c>[Frozen] Periodo</c> no teste, os dois ficam no mesmo período.
    /// </summary>
    private sealed class LancamentosNoPeriodoAutoDataAttribute : DomainAutoDataAttribute
    {
        public LancamentosNoPeriodoAutoDataAttribute()
            : base(fixture =>
            {
                fixture.Register((Periodo periodo, Reais valor, TextoObrigatorio descricao) => NoInicio(periodo, valor, descricao));

                fixture.Register((Banco banco, TipoConta tipo, SufixoConta sufixo, Periodo periodo) => Extrato.Criar(
                    banco,
                    tipo,
                    sufixo,
                    periodo,
                    Enumerable.Range(start: 0, count: 3)
                        .Select(_ => NoInicio(periodo, fixture.Create<Reais>(), fixture.Create<TextoObrigatorio>()))
                        .Select(l => LancamentoContabilizado.Criar(l, l.Data)),
                    saldos: []).Value);
            })
        {
        }

        private static LancamentoConta NoInicio(Periodo periodo, Reais valor, TextoObrigatorio descricao)
        {
            IdentidadeLancamento identidade = IdentidadeLancamento.Composta(periodo.Inicio).ComValor(valor).ComTexto(descricao);
            return LancamentoConta.Criar(periodo.Inicio, hora: null, valor, descricao, contraparte: null, categoriaBanco: null, identidade);
        }
    }
}
