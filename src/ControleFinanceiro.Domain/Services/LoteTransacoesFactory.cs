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
    public static Result<IReadOnlyList<Transacao>> Criar(
        Usuario usuario,
        OrigemTransacao origem,
        IReadOnlyList<LancamentoExtrato> lancamentos)
    {
        Banco? banco = BancoDaOrigem(usuario, origem);
        if (banco is null)
            return Result.Failure<IReadOnlyList<Transacao>>("A origem das transações não pertence ao usuário.");

        // A mensagem cita as posições, e não o identificador: ele é do banco e pode chegar ao modelo.
        int[]? posicoesRepetidas = lancamentos
            .Select((lancamento, indice) => (lancamento.Identidade, Posicao: indice + 1))
            .Where(l => l.Identidade.EhUnica)
            .GroupBy(l => l.Identidade, l => l.Posicao)
            .FirstOrDefault(g => g.Count() > 1)
            ?.ToArray();

        if (posicoesRepetidas is not null)
        {
            string posicoes = $"{string.Join(", ", posicoesRepetidas[..^1])} e {posicoesRepetidas[^1]}";
            return Result.Failure<IReadOnlyList<Transacao>>($"Os lançamentos {posicoes} do lote têm o mesmo identificador do banco.");
        }

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
