using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Application.Abstractions;

/// <summary>
/// O usuário de quem faz a requisição. Cada host (a API, o servidor MCP, o agente) registra o seu adaptador.
/// </summary>
public interface IUsuarioAtual
{
    /// <exception cref="InvalidOperationException">Não há usuário no contexto da execução.</exception>
    IdUsuario Id { get; }
}
