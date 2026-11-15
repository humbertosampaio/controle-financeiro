using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Application.Despesas;

/// <summary>
/// O candidato que a <see cref="CandidatosPagamentoFaturaAppService"/> apontou, com a saída já carregada, para quem monta a
/// resposta ler a data e os textos dela sem buscá-la de novo.
/// </summary>
/// <param name="Saida">A transação do <see cref="CandidatoPagamentoFatura.IdSaida"/>.</param>
public sealed record CandidatoPagamentoFaturaDto(CandidatoPagamentoFatura Candidato, Transacao Saida);
