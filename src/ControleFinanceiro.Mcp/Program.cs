using ControleFinanceiro.Infrastructure.Importacao;
using ControleFinanceiro.Infrastructure.Persistence;
using ControleFinanceiro.Mcp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

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

// O stdout é do protocolo: o appsettings.json manda o log para o stderr e para um arquivo na pasta do servidor.
builder.AddLogging();

try
{
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
}
catch (Exception excecao)
{
    Log.Fatal(excecao, "O servidor MCP terminou com uma exceção.");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
