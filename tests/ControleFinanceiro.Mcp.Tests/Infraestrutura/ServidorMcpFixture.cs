using CliWrap;
using CliWrap.Buffered;
using ControleFinanceiro.AppHost.Resources;
using ModelContextProtocol.Client;
using Npgsql;
using Testcontainers.PostgreSql;

namespace ControleFinanceiro.Mcp.Tests.Infraestrutura;

/// <summary>
/// Sobe um Postgres descartável e o processo real do servidor, como um cliente MCP faria. A conexão vai por
/// variável de ambiente, que vence os User Secrets: nenhum teste chega ao banco local com dados reais.
/// Sem Docker, a fixture falha e os testes falham junto: não são pulados.
/// </summary>
public sealed class ServidorMcpFixture : IAsyncLifetime
{
    private const string VariavelConexao = "ConnectionStrings__controlefinanceiro";

    private static readonly TimeSpan _tempoMaximoExecucao = TimeSpan.FromMinutes(1);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder(PostgresImagem.Completa).Build();

    public static string PastaServidor => AppContext.BaseDirectory;

    private static string DllServidor => Path.Combine(PastaServidor, "ControleFinanceiro.Mcp.dll");

    public McpClient ClienteMcp { get; private set; } = null!;

    public string ConexaoDoContainer => _postgres.GetConnectionString();

    /// <summary>Conexão válida para uma porta em que nada escuta.</summary>
    public string ConexaoPortaFechada => new NpgsqlConnectionStringBuilder(ConexaoDoContainer) { Port = 1, Timeout = 2 }.ToString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        ClienteMcp = await IniciarClienteMcpAsync(ConexaoDoContainer);
    }

    /// <summary>Inicia outro processo do servidor apontando para <paramref name="conexao"/> e faz o handshake.</summary>
    public static Task<McpClient> IniciarClienteMcpAsync(string conexao)
    {
        StdioClientTransport transporte = new(new StdioClientTransportOptions
        {
            Command = "dotnet",
            Arguments = [DllServidor],
            EnvironmentVariables = new Dictionary<string, string?>
            {
                ["DOTNET_ENVIRONMENT"] = "Production",
                [VariavelConexao] = conexao,
            },
        });

        return McpClient.CreateAsync(transporte);
    }

    /// <summary>
    /// Executa o servidor sem cliente MCP, com o stdin já fechado, e devolve como o processo terminou.
    /// Sem o SDK no meio, o stdout chega aqui como o processo o escreveu.
    /// </summary>
    public static async Task<BufferedCommandResult> ExecutarSemClienteMcpAsync(string conexao)
    {
        using CancellationTokenSource limite = new(_tempoMaximoExecucao);

        return await Cli.Wrap("dotnet")
            .WithArguments([DllServidor])
            .WithEnvironmentVariables(new Dictionary<string, string?>
            {
                ["DOTNET_ENVIRONMENT"] = "Production",
                [VariavelConexao] = conexao,
            })
            .WithStandardInputPipe(PipeSource.Null)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(limite.Token);
    }

    public async Task DisposeAsync()
    {
        if (ClienteMcp is not null)
            await ClienteMcp.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
