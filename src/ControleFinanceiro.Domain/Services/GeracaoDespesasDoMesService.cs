using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using CSharpFunctionalExtensions.ValueTasks;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Gera as despesas de um mês a partir das despesas recorrentes: a recorrente guarda o padrão que se repete
/// todo mês, e cada despesa gerada é a desse padrão num mês de referência. Gera só as que faltam: a recorrente que
/// já tem despesa no mês fica de fora, então gerar o mesmo mês de novo não duplica nada.
/// </summary>
public static class GeracaoDespesasDoMesService
{
    /// <param name="cartoesDoUsuario">Os cartões do usuário, entre eles o de cada recorrente no crédito.</param>
    /// <param name="despesasDoMes">As despesas do usuário no mês de referência, geradas ou não por uma recorrente.</param>
    /// <exception cref="ArgumentException">
    /// Quando o cartão de uma recorrente no crédito não está na lista, ou uma despesa da lista é de outro usuário ou
    /// de outro mês.
    /// </exception>
    public static Result<IReadOnlyList<Despesa>> GerarParaMes(
        IReadOnlyCollection<DespesaRecorrente> recorrentes,
        IReadOnlyCollection<Cartao> cartoesDoUsuario,
        IReadOnlyCollection<Despesa> despesasDoMes,
        MesReferencia mesReferencia)
    {
        if (despesasDoMes.Any(d => recorrentes.Any(r => r.IdUsuario != d.IdUsuario)))
            throw new ArgumentException("A lista traz uma despesa de outro usuário.", nameof(despesasDoMes));

        if (despesasDoMes.OfType<DespesaInteira>().Any(d => d.MesReferencia != mesReferencia))
            throw new ArgumentException("A lista traz uma despesa de outro mês.", nameof(despesasDoMes));

        HashSet<IdDespesaRecorrente> recorrentesComDespesa = despesasDoMes
            .Select(d => d.IdDespesaRecorrente)
            .OfType<IdDespesaRecorrente>()
            .ToHashSet();

        Result<Despesa>[] resultados = recorrentes
            .Where(recorrente => !recorrentesComDespesa.Contains(recorrente.Id))
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

    /// <summary>
    /// A despesa do mês de uma recorrente paga sem cartão, ainda pendente: a data e o pagamento projetado ficam no
    /// próprio mês.
    /// </summary>
    /// <remarks>
    /// Lê só a recorrente, mas fica no serviço: movê-lo faria a recorrente criar um agregado de outro tipo, e o par
    /// <see cref="GerarComPagamentoAdiado"/> precisa ficar aqui porque lê também o cartão.
    /// </remarks>
    /// <exception cref="ArgumentException">Quando a despesa recorrente é no crédito.</exception>
    public static Result<Despesa> GerarComPagamentoImediato(DespesaRecorrente despesaRecorrente, MesReferencia mesReferencia)
    {
        if (despesaRecorrente.FormaPagamento == FormaPagamento.Credito)
            throw new ArgumentException("A despesa recorrente no crédito precisa do cartão.", nameof(despesaRecorrente));

        DateOnly dataOcorrencia = mesReferencia.NoDia(despesaRecorrente.DiaOcorrencia);
        return DespesaSimples
            .Prever(
                despesaRecorrente.IdUsuario,
                despesaRecorrente.IdBeneficiario,
                despesaRecorrente.Descricao,
                despesaRecorrente.IdCategoria,
                dataOcorrencia,
                despesaRecorrente.FormaPagamento,
                despesaRecorrente.Valor,
                despesaRecorrente.Id)
            .Map(despesa => despesa as Despesa);
    }

    /// <summary>
    /// A despesa do mês de uma recorrente no crédito: o pagamento fica no vencimento do cartão no mês de referência, e
    /// a data, no mês da compra que cai na fatura desse vencimento, de zero a dois meses antes.
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

        DateOnly dataOcorrencia = cartao.DataOcorrenciaQueVenceEm(mesReferencia, despesaRecorrente.DiaOcorrencia);

        return DespesaAdiada
            .Criar(
                despesaRecorrente.IdUsuario,
                despesaRecorrente.IdBeneficiario,
                despesaRecorrente.Descricao,
                despesaRecorrente.IdCategoria,
                dataOcorrencia,
                despesaRecorrente.Valor,
                cartao.Id,
                cartao.VencimentoNoMes(mesReferencia),
                despesaRecorrente.Id);
    }
}
