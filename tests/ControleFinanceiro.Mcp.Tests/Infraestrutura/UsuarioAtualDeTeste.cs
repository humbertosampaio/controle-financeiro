using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Mcp.Tests.Infraestrutura;

/// <summary>O usuário dono do que os testes gravam pelo <c>DbContext</c>, fora do processo do servidor.</summary>
internal sealed class UsuarioAtualDeTeste : IUsuarioAtual
{
    public UsuarioAtualDeTeste(IdUsuario id)
    {
        Id = id;
    }

    public IdUsuario Id { get; }
}
