using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Usuarios;
using Microsoft.Extensions.Configuration;

namespace ControleFinanceiro.Mcp.Autenticacao;

/// <summary>
/// Tira o usuário atual da configuração <c>Mcp:IdUsuario</c>: o servidor é de um usuário só, e o id não é parâmetro
/// das ferramentas, para o modelo não escolher de quem são os dados. A leitura é feita só quando uma consulta chega
/// ao filtro, para a verificação do servidor funcionar sem a chave.
/// </summary>
internal sealed class UsuarioAtualConfigurado : IUsuarioAtual
{
    public const string Chave = "Mcp:IdUsuario";

    private readonly IConfiguration _configuration;

    public UsuarioAtualConfigurado(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <exception cref="InvalidOperationException">
    /// Quando a chave falta ou não traz um id válido: é erro de configuração, e o filtro nunca é desligado.
    /// </exception>
    public IdUsuario Id
    {
        get
        {
            string? valor = _configuration[Chave];

            if (!Guid.TryParse(valor, out Guid guid) || !IdUsuario.TryFrom(guid, out IdUsuario idUsuario))
                throw new InvalidOperationException($"A configuração '{Chave}' não traz um id de usuário válido.");

            return idUsuario;
        }
    }
}
