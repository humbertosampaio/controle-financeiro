using ControleFinanceiro.Domain.Shared;

namespace ControleFinanceiro.Domain.Transacoes;

/// <summary>
/// O mesmo identificador que o banco do extrato atribuiu ao lançamento aparece em mais de um lançamento do
/// lote: o arquivo é inválido.
/// </summary>
public sealed class ErroIdentificadorRepetido : Erro
{
    /// <summary>As posições, a partir de 1, dos lançamentos repetidos: duas ou mais.</summary>
    public IReadOnlyList<int> Posicoes { get; }

    // A descrição cita as posições, e não o identificador: ele é do banco e pode chegar ao modelo.
    internal ErroIdentificadorRepetido(IReadOnlyList<int> posicoes)
        : base(
            "LoteTransacoes.IdentificadorRepetido",
            $"Os lançamentos {string.Join(", ", posicoes.Take(posicoes.Count - 1))} e {posicoes[^1]} do lote têm o mesmo identificador atribuído pelo banco do extrato.")
    {
        Posicoes = posicoes;
    }
}
