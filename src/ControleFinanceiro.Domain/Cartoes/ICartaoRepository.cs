using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Cartoes;

public interface ICartaoRepository
{
    Task<IReadOnlyList<Cartao>> ListarDoUsuarioAsync(IdUsuario idUsuario, CancellationToken cancellationToken);

    Task AdicionarAsync(Cartao cartao, CancellationToken cancellationToken);
}
