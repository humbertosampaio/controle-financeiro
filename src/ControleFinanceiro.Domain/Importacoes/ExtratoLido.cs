using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Importacoes;

/// <summary>
/// O que o leitor de um layout tirou do arquivo: a conta, o período, os lançamentos na ordem do arquivo e
/// o saldo de cada dia contábil. Da conta, só o banco, o tipo e o sufixo: o número completo, a agência e o
/// titular não saem do leitor.
/// </summary>
public sealed class ExtratoLido
{
    public Banco Banco { get; }
    public TipoConta TipoConta { get; }
    public SufixoConta SufixoConta { get; }
    public PeriodoExtrato Periodo { get; }
    public IReadOnlyList<LancamentoLido> LancamentosLidos { get; }
    public IReadOnlyList<SaldoDiario> Saldos { get; }

    /// <summary>Os lançamentos na ordem do arquivo, como a <see cref="Services.LoteTransacoesFactory"/> os recebe.</summary>
    public IReadOnlyList<LancamentoExtrato> Lancamentos => LancamentosLidos.Select(l => l.Lancamento).ToList();

    private ExtratoLido(
        Banco banco,
        TipoConta tipoConta,
        SufixoConta sufixoConta,
        PeriodoExtrato periodo,
        IReadOnlyList<LancamentoLido> lancamentosLidos,
        IReadOnlyList<SaldoDiario> saldos)
    {
        Banco = banco;
        TipoConta = tipoConta;
        SufixoConta = sufixoConta;
        Periodo = periodo;
        LancamentosLidos = lancamentosLidos;
        Saldos = saldos;
    }

    public static ExtratoLido Criar(
        Banco banco,
        TipoConta tipoConta,
        SufixoConta sufixoConta,
        PeriodoExtrato periodo,
        IEnumerable<LancamentoLido> lancamentosLidos,
        IEnumerable<SaldoDiario> saldos)
    {
        return new(banco, tipoConta, sufixoConta, periodo, lancamentosLidos.ToList(), saldos.ToList());
    }
}
