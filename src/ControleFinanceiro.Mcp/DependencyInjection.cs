using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Mcp.Autenticacao;
using ControleFinanceiro.Mcp.Ferramentas;
using Microsoft.Extensions.DependencyInjection;

namespace ControleFinanceiro.Mcp;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        /// <summary>Registra o servidor MCP, as ferramentas, uma a uma, e os serviços que elas usam.</summary>
        public IServiceCollection AddMcp()
        {
            services
                .AddMcpServer()
                .WithStdioServerTransport()
                .WithTools<VerificarServidorFerramenta>();

            return services.AddScoped<IUsuarioAtual, UsuarioAtualNaoDefinido>();
        }
    }
}
