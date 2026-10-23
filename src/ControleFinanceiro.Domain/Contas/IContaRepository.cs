using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Contas;

public interface IContaRepository
{
    Task<IReadOnlyList<ContaBancaria>> ListarDoUsuarioAsync(IdUsuario idUsuario, CancellationToken cancellationToken);

    Task AdicionarAsync(ContaBancaria conta, CancellationToken cancellationToken);
}
