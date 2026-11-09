using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Liga uma saída a uma despesa e desfaz essa ligação. A saída da conta paga a despesa simples, e a compra do cartão
/// cobra a despesa adiada ou uma parcela, sem pagá-la. Só a despesa muda, e a transação é apenas lida. Uma saída é usada
/// por no máximo uma despesa: o serviço recusa a transação já usada, e os índices únicos são a rede para duas requisições
/// ao mesmo tempo. Se a transação é mesmo a certa cabe a quem vincula, então beneficiário, janela de data e proximidade de
/// valor não bloqueiam.
/// </summary>
public static class VinculoTransacaoService
{
    /// <param name="despesaQueUsaATransacao">
    /// A despesa que já usa a transação, se houver: a que ela cobrou, quando é do cartão, ou uma que ela pagou, quando é da
    /// conta.
    /// </param>
    /// <param name="cartoesDoUsuario">
    /// Todos os cartões do usuário, entre eles o da despesa adiada: a compra de outro cartão da mesma fatura também a cobra.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Quando a transação, a despesa que a usa ou um cartão é de outro usuário, ou quando o cartão da despesa adiada não está
    /// na lista: é bug de quem chama.
    /// </exception>
    /// <exception cref="InvalidOperationException">Quando o tipo da despesa não tem regra de vínculo.</exception>
    public static UnitResult<Erro> Vincular(
        Transacao transacao,
        Despesa despesa,
        Maybe<Despesa> despesaQueUsaATransacao,
        IReadOnlyCollection<Cartao> cartoesDoUsuario)
    {
        if (transacao.IdUsuario != despesa.IdUsuario)
            throw new ArgumentException("A transação é de outro usuário.", nameof(transacao));

        if (despesaQueUsaATransacao.HasValue && despesaQueUsaATransacao.Value.IdUsuario != despesa.IdUsuario)
            throw new ArgumentException("A despesa que usa a transação é de outro usuário.", nameof(despesaQueUsaATransacao));

        if (cartoesDoUsuario.Any(c => c.IdUsuario != despesa.IdUsuario))
            throw new ArgumentException("A lista traz um cartão de outro usuário.", nameof(cartoesDoUsuario));

        if (despesaQueUsaATransacao.HasValue)
            return new ErroTransacaoJaVinculada(despesaQueUsaATransacao.Value.Id);

        if (!transacao.EhSaida)
            return ErroTransacaoNaoEhSaida.Instancia;

        return despesa switch
        {
            DespesaSimples simples => VincularSimples(transacao, simples),
            DespesaAdiada adiada => VincularAdiada(transacao, adiada, cartoesDoUsuario),
            DespesaParcelada parcelada => VincularParcelada(transacao, parcelada),
            _ => throw new InvalidOperationException($"Despesa sem regra de vínculo: {despesa.GetType().Name}"),
        };
    }

    private static UnitResult<Erro> VincularSimples(Transacao transacao, DespesaSimples simples)
    {
        if (transacao.EhParcelada)
            return ErroParcelaIncompativelComDespesa.Instancia;

        if (!transacao.EhOrigemConta)
            return ErroOrigemIncompativelComDespesa.Instancia;

        return simples.PagarCom(transacao.Id, transacao.Data, transacao.ValorAbsoluto);
    }

    /// <remarks>A compra de qualquer cartão da fatura em que a despesa vence a cobra, como os finais do C6.</remarks>
    private static UnitResult<Erro> VincularAdiada(Transacao transacao, DespesaAdiada adiada, IReadOnlyCollection<Cartao> cartoesDoUsuario)
    {
        if (transacao.EhParcelada)
            return ErroParcelaIncompativelComDespesa.Instancia;

        FaturaCartao fatura = FaturaCartao.DoCartao(adiada.IdCartao, cartoesDoUsuario, adiada.MesReferencia);

        if (transacao.Origem.IdCartao is not IdCartao idCartao || !fatura.Contem(idCartao))
            return ErroOrigemIncompativelComDespesa.Instancia;

        return adiada.CobrarCom(new Cobranca(transacao.Id, transacao.ValorAbsoluto));
    }

    /// <remarks>
    /// O mês da parcela não entra na conferência, para um fechamento deslocado por feriado não impedir o vínculo. Interno
    /// para a <see cref="ConciliacaoParcelasService"/>, que já conferiu o usuário, a saída e a transação livre.
    /// </remarks>
    internal static UnitResult<Erro> VincularParcelada(Transacao transacao, DespesaParcelada parcelada)
    {
        if (transacao.Parcela is not Parcela parcela)
            return ErroParcelaIncompativelComDespesa.Instancia;

        if (transacao.Origem.IdCartao != parcelada.IdCartao)
            return ErroOrigemIncompativelComDespesa.Instancia;

        if (!parcelada.EhDaCompra(ChaveCompraParcelada.DaTransacao(transacao)))
            return ErroParcelaIncompativelComDespesa.Instancia;

        return parcelada.CobrarParcela(parcela, new Cobranca(transacao.Id, transacao.ValorAbsoluto));
    }

    /// <summary>
    /// Desfaz o que a transação fez na despesa, pela origem dela: a compra do cartão perde a cobrança, e a saída da conta
    /// deixa de pagar. É a mesma regra das buscas <see cref="Despesa.PagamentoCobradoPor"/> e
    /// <see cref="Despesa.PagamentosRealizadosPor"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Quando a transação é de outro usuário: é bug de quem chama.</exception>
    public static UnitResult<Erro> Desvincular(Transacao transacao, Despesa despesa)
    {
        if (transacao.IdUsuario != despesa.IdUsuario)
            throw new ArgumentException("A transação é de outro usuário.", nameof(transacao));

        return transacao.EhOrigemConta
            ? DesfazerPagamento(transacao.Id, despesa)
            : DesfazerCobranca(transacao.Id, despesa);
    }

    /// <remarks>Por enquanto, só a simples é paga por uma saída da conta.</remarks>
    private static UnitResult<Erro> DesfazerPagamento(IdTransacao idTransacao, Despesa despesa)
    {
        return despesa is DespesaSimples simples
            ? simples.DesfazerPagamento(idTransacao)
            : ErroTransacaoNaoPagouADespesa.Instancia;
    }

    private static UnitResult<Erro> DesfazerCobranca(IdTransacao idTransacao, Despesa despesa)
    {
        return despesa switch
        {
            DespesaAdiada adiada => adiada.DesfazerCobranca(idTransacao),
            DespesaParcelada parcelada => parcelada.DesfazerCobranca(idTransacao),
            _ => ErroTransacaoNaoCobrouADespesa.Instancia,
        };
    }
}
