using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Domain.Importacoes;

/// <summary>
/// O extrato de uma conta: a conta, o período, os lançamentos na ordem do arquivo e o saldo de cada dia
/// contábil. Só existe com os saldos fechando com os lançamentos. Da conta, só o banco, o tipo e o sufixo:
/// o número completo, a agência e o titular não entram nele.
/// </summary>
public sealed class Extrato : ValueObject
{
    public Banco Banco { get; }
    public TipoConta TipoConta { get; }
    public SufixoConta SufixoConta { get; }
    public PeriodoExtrato Periodo { get; }
    public IReadOnlyList<LancamentoContabilizado> LancamentosContabilizados { get; }
    public IReadOnlyList<SaldoDiario> Saldos { get; }

    /// <summary>Os lançamentos na ordem do arquivo, como a <see cref="Services.LoteTransacoesFactory"/> os recebe.</summary>
    public IReadOnlyList<Lancamento> Lancamentos { get; }

    private Extrato(
        Banco banco,
        TipoConta tipoConta,
        SufixoConta sufixoConta,
        PeriodoExtrato periodo,
        List<LancamentoContabilizado> lancamentosContabilizados,
        List<SaldoDiario> saldos)
    {
        Banco = banco;
        TipoConta = tipoConta;
        SufixoConta = sufixoConta;
        Periodo = periodo;
        LancamentosContabilizados = lancamentosContabilizados.AsReadOnly();
        Saldos = saldos.AsReadOnly();
        Lancamentos = lancamentosContabilizados
            .Select(l => l.Lancamento)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Falha quando os lançamentos de algum dia contábil não levam o saldo do dia anterior ao saldo do dia,
    /// quando falta o saldo de um dia ou quando um dia traz mais de um saldo. Uma linha que o leitor perdeu ou
    /// leu com o valor errado faz o saldo não fechar. As mensagens citam só as datas.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Agrupa pela data contábil, porque é por ela que o banco fecha o saldo: um Pix de sábado entra no saldo
    /// do dia útil seguinte.
    /// </para>
    /// <para>
    /// O primeiro dia contábil só serve de base, porque o arquivo não traz o saldo de abertura. Os dias
    /// contábeis depois do fim do período ficam de fora: o banco filtra o período pela data do lançamento, e
    /// esses dias podem ter chegado incompletos.
    /// </para>
    /// </remarks>
    public static Result<Extrato> Criar(
        Banco banco,
        TipoConta tipoConta,
        SufixoConta sufixoConta,
        PeriodoExtrato periodo,
        IEnumerable<LancamentoContabilizado> lancamentosContabilizados,
        IEnumerable<SaldoDiario> saldos)
    {
        List<LancamentoContabilizado> lancamentos = lancamentosContabilizados.ToList();
        List<SaldoDiario> saldosDoExtrato = saldos.ToList();

        return ConferirSaldos(periodo, lancamentos, saldosDoExtrato)
            .Map(() => new Extrato(banco, tipoConta, sufixoConta, periodo, lancamentos, saldosDoExtrato));
    }

    private static Result ConferirSaldos(
        PeriodoExtrato periodo,
        IEnumerable<LancamentoContabilizado> lancamentos,
        IEnumerable<SaldoDiario> saldos)
    {
        List<DateOnly> diasComSaldoRepetido = saldos
            .GroupBy(s => s.DataContabil)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .Order()
            .ToList();

        if (diasComSaldoRepetido.Count > 0)
        {
            return Result.Failure(
                $"O extrato traz mais de um saldo nestes dias: {string.Join(", ", diasComSaldoRepetido.Select(Formatar))}.");
        }

        Dictionary<DateOnly, Reais> saldoDoDia = saldos.ToDictionary(s => s.DataContabil, s => s.Saldo);
        Dictionary<DateOnly, Reais> somas = lancamentos
            .GroupBy(l => l.DataContabil)
            .ToDictionary(g => g.Key, g => (Reais)g.Sum(l => l.Lancamento.Valor));

        List<DateOnly> dias = saldoDoDia.Keys.Union(somas.Keys).Order().ToList();

        foreach ((DateOnly anterior, DateOnly dia) in dias.Zip(dias.Skip(1)).TakeWhile(par => par.Second <= periodo.Fim))
        {
            if (!saldoDoDia.TryGetValue(anterior, out Reais? saldoAnterior))
                return Result.Failure($"O extrato não traz o saldo do dia {Formatar(anterior)}.");

            if (!saldoDoDia.TryGetValue(dia, out Reais? saldo))
                return Result.Failure($"O extrato não traz o saldo do dia {Formatar(dia)}.");

            if (saldoAnterior + somas.GetValueOrDefault(dia, new Reais(0m)) != saldo)
                return Result.Failure($"O saldo do dia {Formatar(dia)} não fecha com os lançamentos do extrato.");
        }

        return Result.Success();
    }

    private static string Formatar(DateOnly data) => data.ToString("d", AppDefaults.CulturaPtBr);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Banco;
        yield return TipoConta;
        yield return SufixoConta;
        yield return Periodo;

        foreach (LancamentoContabilizado lancamento in LancamentosContabilizados)
            yield return lancamento;

        foreach (SaldoDiario saldo in Saldos)
            yield return saldo;
    }
}
