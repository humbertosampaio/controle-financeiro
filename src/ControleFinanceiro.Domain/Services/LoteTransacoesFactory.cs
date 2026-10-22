using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Transforma os lançamentos lidos de um extrato ou de uma fatura nas transações do lote, cada uma com a
/// sua chave de deduplicação.
/// </summary>
public static class LoteTransacoesFactory
{
    public static Result<IReadOnlyList<Transacao>, Erro> Criar(
        Usuario usuario,
        OrigemTransacao origem,
        IReadOnlyList<LancamentoExtrato> lancamentos)
    {
        Banco? banco = BancoDaOrigem(usuario, origem);
        if (banco is null)
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

    private static Banco? BancoDaOrigem(Usuario usuario, OrigemTransacao origem)
    {
        return origem.IdConta is IdConta idConta
            ? usuario.Contas.FirstOrDefault(c => c.Id == idConta)?.Banco
            : usuario.Cartoes.FirstOrDefault(c => c.Id == origem.IdCartao)?.Banco;
    }
}
