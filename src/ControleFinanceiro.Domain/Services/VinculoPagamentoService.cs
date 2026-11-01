using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Registra que uma saída pagou uma despesa ou uma parcela dela. Só a despesa muda, e a transação é apenas lida. Uma saída
/// paga no máximo uma despesa: o serviço recusa a transação já usada, e o índice único de cada tabela é a rede para duas
/// requisições ao mesmo tempo. Se a transação é mesmo a certa cabe a quem vincula, então beneficiário, janela de data e
/// proximidade de valor não bloqueiam.
/// </summary>
public static class VinculoPagamentoService
{
    /// <param name="despesaQueUsaATransacao">A despesa que a transação já paga, se houver.</param>
    /// <exception cref="ArgumentException">Quando a transação ou a despesa que a usa é de outro usuário: é bug de quem chama.</exception>
    /// <exception cref="InvalidOperationException">Quando o tipo da despesa não tem regra de vínculo.</exception>
    public static UnitResult<Erro> Vincular(Transacao transacao, Despesa despesa, Maybe<Despesa> despesaQueUsaATransacao)
    {
        if (transacao.IdUsuario != despesa.IdUsuario)
            throw new ArgumentException("A transação é de outro usuário.", nameof(transacao));

        if (despesaQueUsaATransacao.HasValue && despesaQueUsaATransacao.Value.IdUsuario != despesa.IdUsuario)
            throw new ArgumentException("A despesa que usa a transação é de outro usuário.", nameof(despesaQueUsaATransacao));

        if (despesaQueUsaATransacao.HasValue)
            return new ErroTransacaoJaVinculada(despesaQueUsaATransacao.Value.Id);

        if (!transacao.EhSaida)
            return ErroTransacaoNaoEhSaida.Instancia;

        return despesa switch
        {
            DespesaSimples simples => VincularSimples(transacao, simples),
            DespesaAdiada adiada => VincularAdiada(transacao, adiada),
            DespesaParcelada parcelada => VincularParcelada(transacao, parcelada),
            _ => throw new InvalidOperationException($"Despesa sem regra de vínculo: {despesa.GetType().Name}"),
        };
    }

    private static UnitResult<Erro> VincularSimples(Transacao transacao, DespesaSimples simples)
    {
        if (transacao.EhParcelada)
            return ErroParcelaIncompativelComDespesa.Instancia;

        if (transacao.Origem.Tipo != TipoOrigem.Conta)
            return ErroOrigemIncompativelComDespesa.Instancia;

        return simples.QuitarCom(transacao.Id, transacao.Data, transacao.ValorAbsoluto);
    }

    /// <remarks>A data efetiva é o vencimento, porque a data da transação é a da compra, e não a do pagamento.</remarks>
    private static UnitResult<Erro> VincularAdiada(Transacao transacao, DespesaAdiada adiada)
    {
        if (transacao.EhParcelada)
            return ErroParcelaIncompativelComDespesa.Instancia;

        if (transacao.Origem.IdCartao != adiada.IdCartao)
            return ErroOrigemIncompativelComDespesa.Instancia;

        return adiada.QuitarCom(transacao.Id, adiada.Pagamento.DataProjetada!.Value, transacao.ValorAbsoluto);
    }

    /// <remarks>O mês da parcela não entra na conferência, para um fechamento deslocado por feriado não impedir o vínculo.</remarks>
    private static UnitResult<Erro> VincularParcelada(Transacao transacao, DespesaParcelada parcelada)
    {
        if (transacao.Parcela is not Parcela parcela)
            return ErroParcelaIncompativelComDespesa.Instancia;

        if (transacao.Origem.IdCartao != parcelada.IdCartao)
            return ErroOrigemIncompativelComDespesa.Instancia;

        if (!parcelada.EhDaCompra(ChaveCompraParcelada.DaTransacao(transacao)))
            return ErroParcelaIncompativelComDespesa.Instancia;

        return parcelada.QuitarParcela(parcela, transacao.Id, transacao.ValorAbsoluto);
    }
}
