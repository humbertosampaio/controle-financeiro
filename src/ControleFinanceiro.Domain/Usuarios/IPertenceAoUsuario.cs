namespace ControleFinanceiro.Domain.Usuarios;

/// <summary>
/// Agregado que pertence a um usuário. Toda consulta dele fica restrita ao usuário atual, então quem cria
/// agregado com <see cref="IdUsuario"/> precisa implementar esta interface.
/// </summary>
public interface IPertenceAoUsuario
{
    IdUsuario IdUsuario { get; }
}
