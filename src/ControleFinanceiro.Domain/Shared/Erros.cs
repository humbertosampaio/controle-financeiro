namespace ControleFinanceiro.Domain.Shared;

/// <summary>
/// Os erros tipados do domínio, agrupados por assunto.
/// </summary>
public static class Erros
{
    /// <summary>Falhas da <see cref="Services.LoteTransacoesFactory"/>.</summary>
    public static class LoteTransacoes
    {
        /// <summary>A conta ou o cartão da origem não é do usuário: erro de quem escolheu a origem, não do arquivo.</summary>
        public static readonly Erro OrigemNaoPertenceAoUsuario = new(
            "LoteTransacoes.OrigemNaoPertenceAoUsuario",
            "A origem das transações não pertence ao usuário.");

        /// <summary>
        /// O mesmo identificador que o banco do extrato atribuiu ao lançamento aparece em mais de um
        /// lançamento do lote: o arquivo é inválido.
        /// </summary>
        /// <param name="posicoes">As posições, a partir de 1, dos lançamentos repetidos: duas ou mais.</param>
        internal static Erro IdentificadorRepetido(IReadOnlyList<int> posicoes)
        {
            // A descrição cita as posições, e não o identificador: ele é do banco e pode chegar ao modelo.
            return new(
                "LoteTransacoes.IdentificadorRepetido",
                $"Os lançamentos {string.Join(", ", posicoes.Take(posicoes.Count - 1))} e {posicoes[^1]} do lote têm o mesmo identificador atribuído pelo banco do extrato.");
        }
    }
}
