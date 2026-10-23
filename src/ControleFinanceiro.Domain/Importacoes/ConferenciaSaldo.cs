using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Importacoes;

/// <summary>
/// Confere, dentro do arquivo, que os lançamentos de cada dia contábil levam o saldo do dia anterior ao
/// saldo do dia. Uma linha que o leitor perdeu ou leu com o valor errado faz o saldo não fechar.
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
public static class ConferenciaSaldo
{
    public static Result Conferir(ExtratoLido extrato)
    {
        List<DateOnly> diasComSaldoRepetido = extrato.Saldos
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

        Dictionary<DateOnly, Reais> saldos = extrato.Saldos.ToDictionary(s => s.DataContabil, s => s.Saldo);
        Dictionary<DateOnly, Reais> somas = extrato.LancamentosLidos
            .GroupBy(l => l.DataContabil)
            .ToDictionary(g => g.Key, g => (Reais)g.Sum(l => l.Lancamento.Valor));

        List<DateOnly> dias = saldos.Keys.Union(somas.Keys).Order().ToList();

        foreach ((DateOnly anterior, DateOnly dia) in dias.Zip(dias.Skip(1)).TakeWhile(par => par.Second <= extrato.Periodo.Fim))
        {
            if (!saldos.TryGetValue(anterior, out Reais? saldoAnterior))
                return Result.Failure($"O extrato não traz o saldo do dia {Formatar(anterior)}.");

            if (!saldos.TryGetValue(dia, out Reais? saldo))
                return Result.Failure($"O extrato não traz o saldo do dia {Formatar(dia)}.");

            if (saldoAnterior + somas.GetValueOrDefault(dia, new Reais(0m)) != saldo)
                return Result.Failure($"O saldo do dia {Formatar(dia)} não fecha com os lançamentos do extrato.");
        }

        return Result.Success();
    }

    private static string Formatar(DateOnly data) => data.ToString("d", AppDefaults.CulturaPtBr);
}
