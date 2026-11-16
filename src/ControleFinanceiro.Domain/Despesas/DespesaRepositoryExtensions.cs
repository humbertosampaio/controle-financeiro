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
        public async Task<Maybe<Despesa>> ObterQueUsaAsync(Transacao transacao, CancellationToken cancellationToken)
        {
            if (transacao.EhOrigemConta)
                return (await despesas.ListarPeloPagamentoAsync(transacao.Id, cancellationToken)).TryFirst();

            return await despesas.ObterPelaCobrancaAsync(transacao.Id, cancellationToken);
        }
    }
}
