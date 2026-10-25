using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.WebApi.Autenticacao;
using ControleFinanceiro.WebApi.Endpoints.Importacoes;
using FastEndpoints;
using FastEndpoints.Swagger;

namespace ControleFinanceiro.WebApi;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        /// <summary>Registra os endpoints, a documentação e os serviços que os endpoints usam.</summary>
        public IServiceCollection AddWebApi()
        {
            return services
                .AddFastEndpoints()
                .SwaggerDocument()
                .AddSingleton(TimeProvider.System)
                .AddHttpContextAccessor()
                .AddScoped<IUsuarioAtual, UsuarioAtualRota>()
                .AddScoped<ImportacaoExtratoService>();
        }
    }
}
