using AutoFixture;
using CliWrap;
using CliWrap.Buffered;
using ControleFinanceiro.AppHost.Resources;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Infrastructure.Persistence;
using ControleFinanceiro.Tests.Shared.AutoFixture;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Client;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace ControleFinanceiro.Mcp.Tests.Infraestrutura;

/// <summary>
/// Sobe um Postgres descartável, aplica as migrations reais, cadastra o usuário do servidor e inicia o processo real
/// dele, como um cliente MCP faria. A conexão e o usuário vão por variável de ambiente, que vence os User Secrets:
/// nenhum teste chega ao banco local com dados reais. Sem Docker, a fixture falha e os testes falham junto: não são
/// pulados.
/// </summary>
public sealed class ServidorMcpFixture : IAsyncLifetime
{
    private const string VariavelConexao = "ConnectionStrings__controlefinanceiro";
    private const string VariavelIdUsuario = "Mcp__IdUsuario";

    /// <summary>A meta com que a ferramenta pede ao cliente a aprovação do usuário a cada chamada.</summary>
    public const string MetaInteracaoDoUsuario = "anthropic/requiresUserInteraction";

    /// <summary>
    /// Vence o <c>Mcp:IdUsuario</c> de um User Secret da máquina, que uma variável ausente deixaria passar, sem ser um id
    /// válido.
    /// </summary>
    private const string SemIdUsuario = "sem-usuario";

    private static readonly TimeSpan _tempoMaximoExecucao = TimeSpan.FromMinutes(1);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder(PostgresImagem.Completa).Build();

    private Respawner? _respawner;

    public static string PastaServidor => AppContext.BaseDirectory;

    private static string DllServidor => Path.Combine(PastaServidor, "ControleFinanceiro.Mcp.dll");

    public McpClient ClienteMcp { get; private set; } = null!;

    /// <summary>O usuário que o servidor da fixture enxerga, pela configuração <c>Mcp:IdUsuario</c>.</summary>
    public Usuario Usuario { get; } = new DomainFixture().Create<Usuario>();

    public string ConexaoDoContainer => _postgres.GetConnectionString();

    /// <summary>Conexão válida para uma porta em que nada escuta.</summary>
    public string ConexaoPortaFechada => new NpgsqlConnectionStringBuilder(ConexaoDoContainer) { Port = 1, Timeout = 2 }.ToString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using (ControleFinanceiroDbContext dbContext = CriarDbContext(Usuario.Id))
        {
            await dbContext.Database.MigrateAsync();
            await dbContext.Usuarios.AddAsync(Usuario);
            await dbContext.SaveChangesAsync();
        }

        await using NpgsqlConnection conexao = await AbrirConexaoAsync();
        _respawner = await Respawner.CreateAsync(conexao, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = ["__EFMigrationsHistory", "usuarios", "descontos_salariais"],
        });

        ClienteMcp = await IniciarClienteMcpAsync(ConexaoDoContainer, Usuario.Id);
    }

    /// <summary>
    /// Apaga os dados de todas as tabelas, menos as do usuário. Roda antes de cada teste, não depois: o que um teste
    /// que falhou gravou fica no banco para depuração.
    /// </summary>
    public async Task ResetarBancoAsync()
    {
        if (_respawner is null)
            throw new InvalidOperationException("O banco ainda não subiu.");

        await using NpgsqlConnection conexao = await AbrirConexaoAsync();
        await _respawner.ResetAsync(conexao);
    }

    /// <summary>Um <c>DbContext</c> no banco do container, que enxerga só os dados de <paramref name="idUsuario"/>.</summary>
    internal ControleFinanceiroDbContext CriarDbContext(IdUsuario idUsuario)
    {
        DbContextOptions<ControleFinanceiroDbContext> opcoes = new DbContextOptionsBuilder<ControleFinanceiroDbContext>()
            .UseNpgsql(ConexaoDoContainer)
            .Options;

        return new ControleFinanceiroDbContext(opcoes, new UsuarioAtualDeTeste(idUsuario));
    }

    /// <summary>
    /// Inicia outro processo do servidor apontando para <paramref name="conexao"/> e faz o handshake.
    /// </summary>
    /// <param name="idUsuario">O usuário do servidor, ou nulo para iniciá-lo sem um usuário válido.</param>
    public static Task<McpClient> IniciarClienteMcpAsync(string conexao, IdUsuario? idUsuario)
    {
        StdioClientTransport transporte = new(new StdioClientTransportOptions
        {
            Command = "dotnet",
            Arguments = [DllServidor],
            EnvironmentVariables = new Dictionary<string, string?>
            {
                ["DOTNET_ENVIRONMENT"] = "Production",
                [VariavelConexao] = conexao,
                [VariavelIdUsuario] = idUsuario?.ToString() ?? SemIdUsuario,
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

    private async Task<NpgsqlConnection> AbrirConexaoAsync()
    {
        NpgsqlConnection conexao = new(ConexaoDoContainer);
        await conexao.OpenAsync();
        return conexao;
    }
}
