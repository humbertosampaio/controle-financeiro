using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Domain.Importacoes;

/// <summary>
/// O que o leitor de um layout tirou do arquivo: a conta, o período, os lançamentos na ordem do arquivo e
/// o saldo de cada dia contábil. Da conta, só o banco, o tipo e o sufixo: o número completo, a agência e o
/// titular não saem do leitor.
/// </summary>
public sealed class Extrato
{
    public Banco Banco { get; }
    public TipoConta TipoConta { get; }
    public SufixoConta SufixoConta { get; }
    public PeriodoExtrato Periodo { get; }
    public IReadOnlyList<LancamentoContabilizado> LancamentosContabilizados { get; }
    public IReadOnlyList<SaldoDiario> Saldos { get; }

    /// <summary>Os lançamentos na ordem do arquivo, como a <see cref="Services.LoteTransacoesFactory"/> os recebe.</summary>
    public IReadOnlyList<Lancamento> Lancamentos => LancamentosContabilizados.Select(l => l.Lancamento).ToList();

    private Extrato(
        Banco banco,
        TipoConta tipoConta,
        SufixoConta sufixoConta,
        PeriodoExtrato periodo,
        IReadOnlyList<LancamentoContabilizado> lancamentosContabilizados,
        IReadOnlyList<SaldoDiario> saldos)
    {
        Banco = banco;
        TipoConta = tipoConta;
        SufixoConta = sufixoConta;
        Periodo = periodo;
        LancamentosContabilizados = lancamentosContabilizados;
        Saldos = saldos;
    }

    public static Extrato Criar(
        Banco banco,
        TipoConta tipoConta,
        SufixoConta sufixoConta,
        PeriodoExtrato periodo,
        IEnumerable<LancamentoContabilizado> lancamentosContabilizados,
        IEnumerable<SaldoDiario> saldos)
    {
        return new(banco, tipoConta, sufixoConta, periodo, lancamentosContabilizados.ToList(), saldos.ToList());
    }
}
