using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.WebApi.Autenticacao;

/// <summary>
/// Tira o usuário atual do <c>{idUsuario}</c> da rota, enquanto a API não tem autenticação.
/// Com ela, só este adaptador muda.
/// </summary>
public sealed class UsuarioAtualRota : IUsuarioAtual
{
    public const string ParametroRota = "idUsuario";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public UsuarioAtualRota(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public IdUsuario Id
    {
        get
        {
            HttpContext httpContext = _httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException("Não há requisição em andamento para saber o usuário atual.");

            object? valor = httpContext.Request.RouteValues[ParametroRota];

            if (!Guid.TryParse(valor?.ToString(), out Guid guid) || !IdUsuario.TryFrom(guid, out IdUsuario idUsuario))
                throw new InvalidOperationException($"A rota não traz um '{ParametroRota}' válido.");

            return idUsuario;
        }
    }
}
