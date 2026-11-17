using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Domain.Despesas;

public static class DespesaRepositoryExtensions
{
    extension(IDespesaRepository despesas)
    {
        /// <summary>
        /// A despesa que já usa a transação, pela origem dela: a compra do cartão é usada pela despesa que cobrou, e a saída da
        /// conta, por uma que pagou. É a mesma regra do <see cref="Services.VinculoTransacaoService.Desvincular"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Quando o tipo da transação não tem regra.</exception>
        public async Task<Maybe<Despesa>> ObterQueUsaAsync(Transacao transacao, CancellationToken cancellationToken)
        {
            return transacao switch
            {
                TransacaoConta saidaConta => (await despesas.ListarPeloPagamentoAsync(saidaConta.Id, cancellationToken)).TryFirst(),
                TransacaoCartao compra => await despesas.ObterPelaCobrancaAsync(compra.Id, cancellationToken),
                _ => throw Services.VinculoTransacaoService.TransacaoSemRegra(transacao),
            };
        }
    }
}
