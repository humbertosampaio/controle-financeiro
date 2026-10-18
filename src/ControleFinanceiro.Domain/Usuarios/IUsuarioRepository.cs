namespace ControleFinanceiro.Domain.Usuarios;

public interface IUsuarioRepository
{
    Task AddAsync(Usuario usuario, CancellationToken cancellationToken);
}
