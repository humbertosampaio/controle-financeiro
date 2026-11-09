using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Paga a fatura do cartão com uma saída da conta: as despesas cobradas e pendentes dos cartões da mesma fatura, no mês
/// de vencimento, ficam pagas pela saída, com a data dela e o valor cobrado. Cada despesa muda sozinha, e a transação e
/// os cartões são apenas lidos. Só a ligação é explícita: se a saída é mesmo o pagamento da fatura cabe a quem liga, então
/// valor e data não bloqueiam. Repetir com a mesma saída e a mesma fatura paga as cobranças que chegaram depois. O
/// pagamento se desfaz pela saída, e a cobrança fica.
/// </summary>
public static class PagamentoFaturaService
{
    /// <param name="saida">A saída da conta que pagou a fatura.</param>
    /// <param name="cartao">Um dos cartões da fatura; os demais saem dos cartões do usuário.</param>
    /// <param name="mesVencimento">O mês em que a fatura vence.</param>
    /// <param name="cartoesDoUsuario">Todos os cartões do usuário, e só os dele.</param>
    /// <param name="despesasDoMes">As despesas do usuário com pagamento no mês de vencimento, e só as dele.</param>
    /// <param name="despesasPagasPelaSaida">As despesas que a saída já pagou, inteiras ou por uma das parcelas.</param>
    /// <exception cref="ArgumentException">Quando a saída, o cartão ou um item das listas é de outro usuário: é bug de quem chama.</exception>
    public static Result<PagamentoFatura, Erro> Pagar(
        Transacao saida,
        Cartao cartao,
        MesReferencia mesVencimento,
        IReadOnlyCollection<Cartao> cartoesDoUsuario,
        IReadOnlyCollection<Despesa> despesasDoMes,
        IReadOnlyCollection<Despesa> despesasPagasPelaSaida)
    {
        ConferirUsuario(saida, cartao, cartoesDoUsuario, despesasDoMes, despesasPagasPelaSaida);

        if (!saida.EhSaida)
            return ErroTransacaoNaoEhSaida.Instancia;

        if (!saida.EhOrigemConta)
            return ErroOrigemIncompativelComDespesa.Instancia;

        if (despesasPagasPelaSaida.OfType<DespesaSimples>().FirstOrDefault() is DespesaSimples simples)
            return new ErroTransacaoJaVinculada(simples.Id);

        FaturaCartao fatura = FaturaCartao.DoCartao(cartao, cartoesDoUsuario, mesVencimento);

        if (despesasPagasPelaSaida.Any(d => d is not IDespesaDoCartao doCartao || !doCartao.FoiPagaSoPelaFatura(fatura, saida.Id)))
            return ErroTransacaoPagaOutraFatura.Instancia;

        List<Despesa> pagas = [];
        foreach (Despesa despesa in despesasDoMes)
        {
            if (despesa is IDespesaDoCartao doCartao && doCartao.PagarFatura(fatura, saida.Id, saida.Data))
                pagas.Add(despesa);
        }

        return pagas.Count > 0
            ? new PagamentoFatura(pagas)
            : ErroFaturaSemCobrancaPendente.Instancia;
    }

    /// <summary>
    /// Desfaz o pagamento da fatura pela saída: as despesas do cartão e as parcelas que ela pagou voltam a pendente, com a
    /// cobrança preservada. Cada despesa muda sozinha. A simples paga pela mesma saída fica como está, porque se desfaz
    /// pelo <see cref="VinculoTransacaoService.Desvincular"/>. Na falha, as despesas desfeitas antes da que falhou ficam
    /// alteradas, e quem chama não grava.
    /// </summary>
    /// <param name="saida">A saída da conta que pagou a fatura.</param>
    /// <param name="despesasPagasPelaSaida">
    /// As despesas que a saída pagou, inteiras ou por uma das parcelas. Sem nenhuma do cartão, a saída não pagou fatura, e
    /// a chamada falha com <see cref="ErroTransacaoNaoPagouADespesa"/>.
    /// </param>
    /// <exception cref="ArgumentException">Quando uma despesa da lista é de outro usuário: é bug de quem chama.</exception>
    public static UnitResult<Erro> Desfazer(Transacao saida, IReadOnlyCollection<Despesa> despesasPagasPelaSaida)
    {
        ConferirDespesas(saida, despesasPagasPelaSaida, nameof(despesasPagasPelaSaida));

        IReadOnlyList<IDespesaDoCartao> doCartao = despesasPagasPelaSaida.OfType<IDespesaDoCartao>().ToList();
        if (doCartao.Count == 0)
            return ErroTransacaoNaoPagouADespesa.Instancia;

        foreach (IDespesaDoCartao despesa in doCartao)
        {
            UnitResult<Erro> desfeito = despesa.DesfazerPagamento(saida.Id);
            if (desfeito.IsFailure)
                return desfeito;
        }

        return UnitResult.Success<Erro>();
    }

    private static void ConferirUsuario(
        Transacao saida,
        Cartao cartao,
        IReadOnlyCollection<Cartao> cartoesDoUsuario,
        IReadOnlyCollection<Despesa> despesasDoMes,
        IReadOnlyCollection<Despesa> despesasPagasPelaSaida)
    {
        if (cartao.IdUsuario != saida.IdUsuario)
            throw new ArgumentException("O cartão é de outro usuário.", nameof(cartao));

        if (cartoesDoUsuario.Any(c => c.IdUsuario != saida.IdUsuario))
            throw new ArgumentException("A lista traz um cartão de outro usuário.", nameof(cartoesDoUsuario));

        ConferirDespesas(saida, despesasDoMes, nameof(despesasDoMes));
        ConferirDespesas(saida, despesasPagasPelaSaida, nameof(despesasPagasPelaSaida));
    }

    private static void ConferirDespesas(Transacao saida, IReadOnlyCollection<Despesa> despesas, string nomeParametro)
    {
        if (despesas.Any(d => d.IdUsuario != saida.IdUsuario))
            throw new ArgumentException("A lista traz uma despesa de outro usuário.", nomeParametro);
    }
}
