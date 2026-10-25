using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using CSharpFunctionalExtensions.ValueTasks;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Gera as despesas de um mês a partir das despesas recorrentes: a recorrente guarda o padrão que se repete
/// todo mês, e cada despesa gerada é a desse padrão num mês de referência.
/// </summary>
public static class GeracaoDespesasDoMesService
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

        return resultados
            .Combine()
            .Map(IReadOnlyList<Despesa> (despesas) => despesas.ToArray());
    }

    private static Result<Despesa> Gerar(
        DespesaRecorrente recorrente,
        IReadOnlyCollection<Cartao> cartoesDoUsuario,
        MesReferencia mesReferencia)
    {
        if (recorrente.IdCartao is not IdCartao idCartao)
            return GerarComPagamentoImediato(recorrente, mesReferencia);

        Cartao cartao = cartoesDoUsuario.FirstOrDefault(c => c.Id == idCartao)
            ?? throw new ArgumentException("O cartão de uma despesa recorrente não está na lista.", nameof(cartoesDoUsuario));

        return GerarComPagamentoAdiado(recorrente, cartao, mesReferencia);
    }

    /// <summary>A despesa do mês de uma recorrente paga sem cartão: a data e o pagamento ficam no próprio mês.</summary>
    /// <exception cref="ArgumentException">Quando a despesa recorrente é no crédito.</exception>
    public static Result<Despesa> GerarComPagamentoImediato(DespesaRecorrente despesaRecorrente, MesReferencia mesReferencia)
    {
        if (despesaRecorrente.FormaPagamento == FormaPagamento.Credito)
            throw new ArgumentException("A despesa recorrente no crédito precisa do cartão.", nameof(despesaRecorrente));

        DateTimeOffset dataOcorrencia = mesReferencia.NoDia(despesaRecorrente.DiaOcorrencia).ToLocalDateTimeOffset();
        return DespesaSimples
            .Criar(
                despesaRecorrente.IdUsuario,
                despesaRecorrente.IdBeneficiario,
                despesaRecorrente.Descricao,
                despesaRecorrente.IdCategoria,
                dataOcorrencia,
                despesaRecorrente.FormaPagamento,
                despesaRecorrente.Valor)
            .Map(despesa => despesa as Despesa);
    }

    /// <summary>
    /// A despesa do mês de uma recorrente no crédito: a data fica no mês anterior, e o pagamento, no vencimento
    /// do cartão no mês de referência.
    /// </summary>
    /// <param name="cartao">
    /// O cartão da despesa recorrente. A posse foi conferida na criação da recorrente: outro cartão é erro de
    /// quem chama.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Quando a despesa recorrente não é no crédito ou o cartão não é o dela.
    /// </exception>
    public static Result<Despesa> GerarComPagamentoAdiado(
        DespesaRecorrente despesaRecorrente,
        Cartao cartao,
        MesReferencia mesReferencia)
    {
        if (despesaRecorrente.FormaPagamento != FormaPagamento.Credito)
            throw new ArgumentException("Só a despesa recorrente no crédito tem cartão.", nameof(despesaRecorrente));

        if (cartao.Id != despesaRecorrente.IdCartao)
            throw new ArgumentException("O cartão não é o da despesa recorrente.", nameof(cartao));

        DateTimeOffset dataOcorrencia = mesReferencia.Anterior()
            .NoDia(despesaRecorrente.DiaOcorrencia)
            .ToLocalDateTimeOffset();

        return DespesaAdiada
            .Criar(
                despesaRecorrente.IdUsuario,
                despesaRecorrente.IdBeneficiario,
                despesaRecorrente.Descricao,
                despesaRecorrente.IdCategoria,
                dataOcorrencia,
                despesaRecorrente.Valor,
                cartao.DiaVencimento);
    }
}
