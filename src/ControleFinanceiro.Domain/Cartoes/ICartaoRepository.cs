namespace ControleFinanceiro.Domain.Cartoes;

/// <summary>Enxerga só os cartões do usuário atual.</summary>
public interface ICartaoRepository
{
    Task<IReadOnlyList<Cartao>> ListarAsync(CancellationToken cancellationToken);

    /// <summary>O cartão de outro usuário não é encontrado, como o que não existe.</summary>
    Task<Maybe<Cartao>> ObterAsync(IdCartao idCartao, CancellationToken cancellationToken);

    Task AdicionarAsync(Cartao cartao, CancellationToken cancellationToken);
}
