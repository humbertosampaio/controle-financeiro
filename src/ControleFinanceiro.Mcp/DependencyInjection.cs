using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Application.IA.Escritas;
using ControleFinanceiro.Application.IA.Leituras;
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
                .WithTools<VerificarServidorTool>()
                .WithTools<ListarTransacoesTool>()
                .WithTools<ListarDespesasFixasTool>()
                .WithTools<BalancoMensalTool>()
                .WithTools<VincularTransacaoTool>();

            return services
                .AddScoped<IUsuarioAtual, UsuarioAtualConfigurado>()
                .AddScoped<LeituraParaModeloService>()
                .AddScoped<VinculoParaModeloService>();
        }
    }
}
