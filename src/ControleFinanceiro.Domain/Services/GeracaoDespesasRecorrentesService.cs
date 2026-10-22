using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Services;

public static class GeracaoDespesasRecorrentesService
{
    /// <param name="cartoesDoUsuario">Os cartões do usuário, entre eles o de cada recorrente no crédito.</param>
    /// <exception cref="ArgumentException">Quando o cartão de uma recorrente no crédito não está na lista.</exception>
    public static Result<IReadOnlyList<Despesa>> GerarParaMes(
        IReadOnlyCollection<DespesaRecorrente> recorrentes,
        IReadOnlyCollection<Cartao> cartoesDoUsuario,
        MesReferencia mesReferencia)
    {
        Result<Despesa>[] resultados = recorrentes
            .Select(recorrente => Gerar(recorrente, cartoesDoUsuario, mesReferencia))
            .ToArray();

        return Result
            .Combine(resultados)
            .Map<IReadOnlyList<Despesa>>(() => resultados.Select(r => r.Value).ToArray());
    }

    private static Result<Despesa> Gerar(
        DespesaRecorrente recorrente,
        IReadOnlyCollection<Cartao> cartoesDoUsuario,
        MesReferencia mesReferencia)
    {
        if (recorrente.IdCartao is not IdCartao idCartao)
            return DespesaFactory.CriarComPagamentoImediato(recorrente, mesReferencia);

        Cartao cartao = cartoesDoUsuario.FirstOrDefault(c => c.Id == idCartao)
            ?? throw new ArgumentException("O cartão de uma despesa recorrente não está na lista.", nameof(cartoesDoUsuario));

        return DespesaFactory.CriarComPagamentoAdiado(recorrente, cartao, mesReferencia);
    }
}
