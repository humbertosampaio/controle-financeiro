using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Infrastructure.Persistence;

/// <summary>
/// Usuário atual do <c>dotnet ef</c>, que monta o modelo e gera migrations sem consultar entidades.
/// Se alguma consulta chegar ao filtro, é bug: lança em vez de desligar o filtro.
/// </summary>
internal sealed class UsuarioAtualIndisponivel : IUsuarioAtual
{
    public IdUsuario Id => throw new InvalidOperationException("Não há usuário atual fora de uma requisição.");
}
