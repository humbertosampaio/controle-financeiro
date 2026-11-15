namespace ControleFinanceiro.Domain.Usuarios;

public static class PertenceAoUsuarioExtensions
{
    extension<T>(IReadOnlyCollection<T> itens)
        where T : IPertenceAoUsuario
    {
        /// <summary>
        /// Confere que todo item da lista é do usuário. A lista com item de outro usuário é bug de quem chama, e não falha
        /// prevista.
        /// </summary>
        /// <param name="nomeParametro">O nome do parâmetro que recebeu a lista, para a exceção apontá-lo.</param>
        /// <exception cref="ArgumentException">Quando um item é de outro usuário.</exception>
        public void ConferirDono(IdUsuario idUsuario, string nomeParametro)
        {
            if (itens.Any(item => item.IdUsuario != idUsuario))
                throw new ArgumentException("A lista traz um item de outro usuário.", nomeParametro);
        }
    }
}
