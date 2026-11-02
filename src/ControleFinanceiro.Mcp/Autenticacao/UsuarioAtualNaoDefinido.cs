using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Mcp.Autenticacao;

/// <summary>
/// O servidor ainda não identifica o usuário, e nenhuma ferramenta consulta entidades.
/// Se alguma consulta chegar ao filtro, é bug: lança em vez de desligar o filtro.
/// </summary>
internal sealed class UsuarioAtualNaoDefinido : IUsuarioAtual
{
    public IdUsuario Id => throw new InvalidOperationException("O servidor MCP ainda não identifica o usuário atual.");
}
