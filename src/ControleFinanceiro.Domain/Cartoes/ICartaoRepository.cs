using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Cartoes;

public interface ICartaoRepository
{
    Task<IReadOnlyList<Cartao>> ListarDoUsuarioAsync(IdUsuario idUsuario, CancellationToken cancellationToken);

    Task AddAsync(Cartao cartao, CancellationToken cancellationToken);
}
