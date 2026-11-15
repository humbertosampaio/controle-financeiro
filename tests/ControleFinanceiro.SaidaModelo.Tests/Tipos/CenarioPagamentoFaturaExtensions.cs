using ControleFinanceiro.Application.Despesas;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared;

namespace ControleFinanceiro.SaidaModelo.Tests.Tipos;

/// <summary>O candidato que os testes de payload serializam, montado pelo serviço de domínio.</summary>
internal static class CenarioPagamentoFaturaExtensions
{
    extension(CenarioPagamentoFatura cenario)
    {
        /// <summary>A fatura de 100,00 do cartão e a saída de 100,00 no dia do vencimento: o candidato único.</summary>
        public CandidatoPagamentoFaturaDto CandidatoPagamentoFaturaUnico(Cartao cartao, VencimentoFatura vencimento, Lancamento compra, Lancamento linhaSaida)
        {
            cenario.ImportarFatura(cartao, vencimento, compra.NoCartaoComValor(-100m));
            Transacao saida = cenario.Saida(linhaSaida, vencimento, (ReaisPositivo)100m);
            CandidatoPagamentoFatura candidato = cenario.Apontar([cartao], vencimento.Mes).Single();

            return new CandidatoPagamentoFaturaDto(candidato, saida);
        }
    }
}
