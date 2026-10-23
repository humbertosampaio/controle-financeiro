namespace ControleFinanceiro.Domain.Usuarios;

public interface IUsuarioRepository
{
    Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken);

    Task<Maybe<Usuario>> ObterPorIdAsync(IdUsuario id, CancellationToken cancellationToken);
}
