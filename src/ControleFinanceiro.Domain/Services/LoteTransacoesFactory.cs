using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Transforma os lançamentos lidos de um extrato ou de uma fatura nas transações do lote, cada uma com a
/// sua chave de deduplicação. A origem e o banco da chave saem da conta ou do cartão, e cada transação fica
/// ligada à importação do arquivo.
/// </summary>
public static class LoteTransacoesFactory
{
    /// <param name="importacao">A importação do arquivo, da mesma conta.</param>
    /// <exception cref="ArgumentException">Quando a importação é de outra origem.</exception>
    public static Result<IReadOnlyList<Transacao>, Erro> Criar(
        IdUsuario idUsuario,
        ContaBancaria conta,
        ImportacaoExtrato importacao,
        IReadOnlyList<LancamentoExtrato> lancamentos)
    {
        return Criar(idUsuario, conta.IdUsuario, conta.Banco, OrigemTransacao.DaConta(conta.Id), importacao, lancamentos);
    }

    /// <param name="importacao">A importação do arquivo, do mesmo cartão.</param>
    /// <exception cref="ArgumentException">Quando a importação é de outra origem.</exception>
    public static Result<IReadOnlyList<Transacao>, Erro> Criar(
        IdUsuario idUsuario,
        Cartao cartao,
        ImportacaoExtrato importacao,
        IReadOnlyList<LancamentoExtrato> lancamentos)
    {
        return Criar(idUsuario, cartao.IdUsuario, cartao.Banco, OrigemTransacao.DoCartao(cartao.Id), importacao, lancamentos);
    }

    private static Result<IReadOnlyList<Transacao>, Erro> Criar(
        IdUsuario idUsuario,
        IdUsuario donoDaOrigem,
        Banco banco,
        OrigemTransacao origem,
        ImportacaoExtrato importacao,
        IReadOnlyList<LancamentoExtrato> lancamentos)
    {
        if (donoDaOrigem != idUsuario)
            return ErroOrigemNaoPertenceAoUsuario.Instancia;

        if (importacao.Origem != origem)
            throw new ArgumentException("A importação é de outra origem.", nameof(importacao));

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
            .Zip(chaves, (lancamento, chave) => Transacao.Criar(idUsuario, importacao.Id, origem, lancamento, chave))
            .ToList();
    }
}
