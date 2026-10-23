using AutoFixture.Kernel;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

/// <summary>
/// Monta o <see cref="Extrato"/> pelo <c>Criar</c>, que devolve <c>Result</c>. Todos os lançamentos entram
/// no saldo do primeiro dia do período, que é o único saldo do extrato: com um dia contábil só, não há saldo
/// a fechar, e o extrato é sempre válido. Com <c>[Frozen] PeriodoExtrato</c> no teste, o extrato usa esse
/// período.
/// </summary>
public sealed class ExtratoGenerator : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type t || t != typeof(Extrato))
            return new NoSpecimen();

        PeriodoExtrato periodo = context.Create<PeriodoExtrato>();

        return Extrato
            .Criar(
                context.Create<Banco>(),
                context.Create<TipoConta>(),
                context.Create<SufixoConta>(),
                periodo,
                context.CreateMany<Lancamento>().Select(l => LancamentoContabilizado.Criar(l, periodo.Inicio)),
                [SaldoDiario.Criar(periodo.Inicio, context.Create<Reais>())])
            .Value;
    }
}
