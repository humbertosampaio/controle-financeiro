using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Domain.Importacoes;

/// <summary>
/// Confere, antes de gravar, que a soma das transações da origem com a data no período fica igual à soma
/// dos lançamentos do extrato. O banco filtra o período pela data do lançamento, então o arquivo traz
/// exatamente as transações do período.
/// </summary>
/// <remarks>
/// A falha é um aviso, e não impede a gravação: ela aparece quando o período cruza algo já gravado que o
/// arquivo não reconheceu, como uma chave mudada por layout ou um lançamento pendente que trocou de
/// descrição, ou quando o arquivo traz um lançamento fora do período.
/// </remarks>
public static class ConferenciaSomaPeriodo
{
    /// <param name="gravadasNoPeriodo">As transações da origem já gravadas, com a data no período do extrato.</param>
    /// <param name="novas">As transações do extrato que ainda não estavam gravadas.</param>
    public static Result Conferir(ExtratoLido extrato, IEnumerable<Transacao> gravadasNoPeriodo, IEnumerable<Transacao> novas)
    {
        Reais somaDoExtrato = extrato.Lancamentos.Sum(l => l.Valor);
        Reais somaGravada = gravadasNoPeriodo
            .Concat(novas.Where(t => extrato.Periodo.Contem(t.Data)))
            .Sum(t => t.Valor);

        return Result.SuccessIf(
            somaGravada == somaDoExtrato,
            $"A soma das transações gravadas de {Formatar(extrato.Periodo.Inicio)} a {Formatar(extrato.Periodo.Fim)} "
                + "não fecha com a soma dos lançamentos do extrato.");
    }

    private static string Formatar(DateOnly data) => data.ToString("d", AppDefaults.CulturaPtBr);
}
