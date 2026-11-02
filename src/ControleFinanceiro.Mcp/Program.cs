using ControleFinanceiro.Infrastructure.Persistence;
using ControleFinanceiro.Mcp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// O diretório de trabalho é o que o cliente MCP escolher: a raiz do conteúdo não pode depender dele.
HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

// O cliente MCP inicia o processo fora de Development, onde o host não carrega User Secrets.
// As variáveis de ambiente vêm depois para continuarem vencendo o secret.
builder.Configuration
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables();

// O stdout é do protocolo: todo log vai para o stderr, e só o que pede atenção.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

builder.Services
    .AddInfrastructurePersistence(builder.Configuration)
    .AddMcp();

await builder.Build().RunAsync();
