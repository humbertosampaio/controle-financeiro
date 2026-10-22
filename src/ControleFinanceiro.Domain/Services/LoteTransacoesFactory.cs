using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Transforma os lançamentos lidos de um extrato ou de uma fatura nas transações do lote, cada uma com a
/// sua chave de deduplicação. A origem e o banco da chave saem da conta ou do cartão.
/// </summary>
public static class LoteTransacoesFactory
{
    public static Result<IReadOnlyList<Transacao>, Erro> Criar(
        IdUsuario idUsuario,
        ContaBancaria conta,
        IReadOnlyList<LancamentoExtrato> lancamentos)
    {
        return Criar(idUsuario, conta.IdUsuario, conta.Banco, OrigemTransacao.DaConta(conta.Id), lancamentos);
    }

    public static Result<IReadOnlyList<Transacao>, Erro> Criar(
        IdUsuario idUsuario,
        Cartao cartao,
        IReadOnlyList<LancamentoExtrato> lancamentos)
    {
        return Criar(idUsuario, cartao.IdUsuario, cartao.Banco, OrigemTransacao.DoCartao(cartao.Id), lancamentos);
    }

    private static Result<IReadOnlyList<Transacao>, Erro> Criar(
        IdUsuario idUsuario,
        IdUsuario donoDaOrigem,
        Banco banco,
        OrigemTransacao origem,
        IReadOnlyList<LancamentoExtrato> lancamentos)
    {
        if (donoDaOrigem != idUsuario)
            return ErroOrigemNaoPertenceAoUsuario.Instancia;

        int[]? posicoesRepetidas = lancamentos
            .Select((lancamento, indice) => (lancamento.Identidade, Posicao: indice + 1))
            .Where(l => l.Identidade.EhUnica)
            .GroupBy(l => l.Identidade, l => l.Posicao)
            .FirstOrDefault(g => g.Count() > 1)
            ?.ToArray();

        if (posicoesRepetidas is not null)
            return new ErroIdentificadorRepetido(posicoesRepetidas);

        IReadOnlyList<ChaveDeduplicacao> chaves = ChaveDeduplicacao.CalcularLote(banco, origem, lancamentos.Select(l => l.Identidade));

        return lancamentos
            .Zip(chaves, (lancamento, chave) => Transacao.Criar(origem, lancamento, chave))
            .ToList();
    }
}
