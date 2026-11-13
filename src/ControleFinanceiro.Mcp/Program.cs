using ControleFinanceiro.Infrastructure.Importacao;
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

// A pasta dados/ do repositório, de onde a cópia publicada em artifacts/mcp/ sobe duas pastas. Fora dela, como no bin
// do src ou dos testes, a pasta padrão não existe e a importação falha fechado: nunca se procura o repositório subindo
// pastas, porque nos testes isso acharia os dados reais.
string origemImportacao = builder.Configuration["Mcp:OrigemArquivoImportacao"]
    ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "dados"));

builder.Services
    .AddInfrastructurePersistence(builder.Configuration)
    .AddInfrastructureImportacao()
    .AddOrigemArquivoImportacao(origemImportacao)
    .AddMcp();

await builder.Build().RunAsync();
