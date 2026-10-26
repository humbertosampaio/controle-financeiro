using ControleFinanceiro.AppHost.Resources;
using ControleFinanceiro.Infrastructure.Persistence;
using ControleFinanceiro.WebApi.Endpoints.Categorias.CadastrarLote;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.AdicionarCartao;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using System.Net;
using System.Net.Http.Json;
using Testcontainers.PostgreSql;

namespace ControleFinanceiro.WebApi.Tests.Infraestrutura;

/// <summary>
/// Sobe um Postgres descartável e a API em memória apontando para ele, uma vez para a suíte inteira.
/// O schema vem das migrations reais, aplicadas pelo <c>Program.cs</c> no ambiente Development.
/// Sem Docker, a fixture falha e os testes falham junto: não são pulados.
/// </summary>
public sealed class ApiComPostgresFixture : IAsyncLifetime
{
    private static readonly Uri _rotaCadastro = new("/api/usuarios", UriKind.Relative);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder(PostgresImagem.Completa).Build();

    private WebApplicationFactory<Program>? _factory;
    private Respawner? _respawner;

    public HttpClient Client { get; private set; } = null!;

    public string ConexaoDoContainer => _postgres.GetConnectionString();

    private WebApplicationFactory<Program> Factory =>
        _factory ?? throw new InvalidOperationException("A API ainda não subiu.");

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder
                .UseEnvironment("Development")
                .UseSetting("ConnectionStrings:controlefinanceiro", ConexaoDoContainer));

        // Criar o client sobe a API e, com ela, aplica as migrations.
        Client = _factory.CreateClient();

        // Em Development a API também lê os User Secrets, onde está a conexão do banco local com dados
        // reais. Se um dia ela vencer o UseSetting acima, nenhum teste pode rodar.
        string? conexaoDaApi = await ConsultarAsync(db => Task.FromResult(db.Database.GetConnectionString()));
        if (!MesmoBanco(conexaoDaApi, ConexaoDoContainer))
            throw new InvalidOperationException("A API dos testes não está usando o Postgres do container.");

        await using NpgsqlConnection conexao = await AbrirConexaoAsync();
        _respawner = await Respawner.CreateAsync(conexao, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = ["__EFMigrationsHistory"],
        });
    }

    /// <summary>
    /// Apaga os dados de todas as tabelas. Roda antes de cada teste, não depois: o que um teste que
    /// falhou gravou fica no banco para depuração.
    /// </summary>
    public async Task ResetarBancoAsync()
    {
        if (_respawner is null)
            throw new InvalidOperationException("A API ainda não subiu.");

        await using NpgsqlConnection conexao = await AbrirConexaoAsync();
        await _respawner.ResetAsync(conexao);
    }

    /// <summary>Cadastra um usuário pela API, conferindo o 201, e devolve o id dele.</summary>
    public async Task<Guid> CadastrarUsuarioAsync(CadastrarUsuarioRequest cadastro)
    {
        using HttpResponseMessage resposta = await Client.PostAsJsonAsync(_rotaCadastro, cadastro);
        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        CadastrarUsuarioResponse? corpo = await resposta.Content.ReadFromJsonAsync<CadastrarUsuarioResponse>();
        return corpo!.Id;
    }

    /// <summary>Adiciona um cartão ao usuário pela API, conferindo o 201, e devolve o id dele.</summary>
    public async Task<Guid> AdicionarCartaoAsync(Guid idUsuario, AdicionarCartaoRequest cartao)
    {
        using HttpResponseMessage resposta = await Client.PostAsJsonAsync(
            new Uri($"/api/usuarios/{idUsuario}/cartoes", UriKind.Relative),
            cartao);
        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        AdicionarCartaoResponse? corpo = await resposta.Content.ReadFromJsonAsync<AdicionarCartaoResponse>();
        return corpo!.Id;
    }

    /// <summary>
    /// Cadastra categorias do usuário pela API, conferindo o 200, e devolve a categoria de cada item, na ordem dos itens.
    /// </summary>
    public async Task<IReadOnlyList<Guid>> CadastrarCategoriasAsync(Guid idUsuario, CadastrarLoteCategoriasRequest categorias)
    {
        using HttpResponseMessage resposta = await Client.PostAsJsonAsync(
            new Uri($"/api/usuarios/{idUsuario}/categorias/lote", UriKind.Relative),
            categorias);
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        CadastrarLoteCategoriasResponse? corpo = await resposta.Content.ReadFromJsonAsync<CadastrarLoteCategoriasResponse>();
        return corpo!.IdsCategorias;
    }

    /// <summary>
    /// Executa <paramref name="consulta"/> num escopo novo, com um DbContext sem nada rastreado:
    /// o que ele lê veio do banco, não da memória da requisição.
    /// </summary>
    public async Task<T> ConsultarAsync<T>(Func<ControleFinanceiroDbContext, Task<T>> consulta)
    {
        await using AsyncServiceScope escopo = Factory.Services.CreateAsyncScope();
        ControleFinanceiroDbContext dbContext = escopo.ServiceProvider.GetRequiredService<ControleFinanceiroDbContext>();
        return await consulta(dbContext);
    }

    /// <summary>
    /// Todas as linhas de uma <paramref name="tabela"/>, lidas sem rastreio num escopo novo. Ignora o filtro do
    /// usuário atual: a leitura roda fora de requisição, sem usuário, e o teste confere a tabela inteira.
    /// </summary>
    public Task<List<T>> ObterTodosAsync<T>(Func<ControleFinanceiroDbContext, IQueryable<T>> tabela)
        where T : class
    {
        return ConsultarAsync(db => tabela(db)
            .IgnoreQueryFilters([ControleFinanceiroDbContext.FiltroUsuario])
            .AsNoTracking()
            .ToListAsync());
    }

    /// <summary>
    /// Todos os serviços do tipo <typeparamref name="T"/> registrados no container da API, resolvidos num escopo
    /// novo: confere a composição do <c>Program.cs</c>, e não a de um método de registro isolado.
    /// </summary>
    public IReadOnlyList<T> ObterServicos<T>()
    {
        using IServiceScope escopo = Factory.Services.CreateScope();
        return escopo.ServiceProvider.GetServices<T>().ToList();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (_factory is not null)
            await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    internal static bool MesmoBanco(string? conexao, string outra)
    {
        if (string.IsNullOrWhiteSpace(conexao))
            return false;

        NpgsqlConnectionStringBuilder a = new(conexao);
        NpgsqlConnectionStringBuilder b = new(outra);
        return a.Host == b.Host && a.Port == b.Port && a.Database == b.Database;
    }

    private async Task<NpgsqlConnection> AbrirConexaoAsync()
    {
        NpgsqlConnection conexao = new(ConexaoDoContainer);
        await conexao.OpenAsync();
        return conexao;
    }
}
