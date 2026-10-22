using ControleFinanceiro.Domain.Shared;

namespace ControleFinanceiro.Domain.Transacoes;

/// <summary>
/// A conta ou o cartão da origem não é do usuário: erro de quem escolheu a origem, não do arquivo.
/// </summary>
public sealed class ErroOrigemNaoPertenceAoUsuario : Erro
{
    public static readonly ErroOrigemNaoPertenceAoUsuario Instancia = new();

    internal ErroOrigemNaoPertenceAoUsuario()
        : base("LoteTransacoes.OrigemNaoPertenceAoUsuario", "A origem das transações não pertence ao usuário.")
    {
    }
}
