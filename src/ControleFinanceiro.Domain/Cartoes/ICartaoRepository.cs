using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Cartoes;

public interface ICartaoRepository
{
    Task<IReadOnlyList<Cartao>> ListarDoUsuarioAsync(IdUsuario idUsuario, CancellationToken cancellationToken);

    /// <summary>O cartão de outro usuário não é encontrado, como o que não existe.</summary>
    Task<Maybe<Cartao>> ObterDoUsuarioAsync(IdUsuario idUsuario, IdCartao idCartao, CancellationToken cancellationToken);

    Task AdicionarAsync(Cartao cartao, CancellationToken cancellationToken);
}
