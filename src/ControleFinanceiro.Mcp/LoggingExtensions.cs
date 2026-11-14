using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace ControleFinanceiro.Mcp;

public static class LoggingExtensions
{
    private const string SecaoSinks = "Serilog:WriteTo";
    private const string ChaveCaminho = "path";

    extension(HostApplicationBuilder builder)
    {
        /// <summary>
        /// Registra o Serilog com os sinks e os níveis da seção <c>Serilog</c> da configuração. O logger nasce antes dos
        /// serviços, para que uma falha na partida também chegue ao arquivo; quem chama encerra com
        /// <see cref="Log.CloseAndFlushAsync"/>, que esvazia o sink assíncrono.
        /// </summary>
        public HostApplicationBuilder AddLogging()
        {
            AncorarCaminhosRelativos(builder.Configuration, builder.Environment.ContentRootPath);

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .CreateLogger();
            builder.Services.AddSerilog();

            return builder;
        }
    }

    /// <summary>
    /// O sink resolve um caminho relativo pelo diretório de trabalho, que é escolha do cliente MCP: aqui ele passa a
    /// partir da raiz do conteúdo, a pasta do servidor. Um caminho absoluto fica como está. O <c>%VAR%</c> de um caminho
    /// relativo não é expandido antes da âncora, e por isso não é aceito nele.
    /// </summary>
    private static void AncorarCaminhosRelativos(IConfiguration configuracao, string raiz)
    {
        KeyValuePair<string, string?>[] caminhosRelativos = configuracao.GetSection(SecaoSinks)
            .AsEnumerable()
            .Where(item => string.Equals(ConfigurationPath.GetSectionKey(item.Key), ChaveCaminho, StringComparison.OrdinalIgnoreCase))
            .Where(item => !string.IsNullOrWhiteSpace(item.Value) && !Path.IsPathFullyQualified(item.Value))
            .ToArray();

        foreach ((string chave, string? caminho) in caminhosRelativos)
            configuracao[chave] = Path.GetFullPath(caminho!, raiz);
    }
}
