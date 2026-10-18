using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.WebApi.Tests.Infraestrutura;

[Collection(ApiCollection.Nome)]
public sealed class BancoDeDadosTests
{
    private readonly ApiComPostgresFixture _api;

    public BancoDeDadosTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    [Fact]
    public async Task Conexao_Quando_ApiSobe_Deve_SerADoContainer()
    {
        string? conexaoDaApi = await _api.ConsultarAsync(db => Task.FromResult(db.Database.GetConnectionString()));

        ApiComPostgresFixture.MesmoBanco(conexaoDaApi, _api.ConexaoDoContainer).Should().BeTrue(
            "os testes nunca podem gravar no banco local, que tem dados reais");
    }

    [Fact]
    public async Task Migrations_Quando_ApiSobe_Deve_EstarTodasAplicadas()
    {
        (IEnumerable<string> existentes, IEnumerable<string> aplicadas) = await _api.ConsultarAsync(async db =>
            (db.Database.GetMigrations(), await db.Database.GetAppliedMigrationsAsync()));

        aplicadas.Should().NotBeEmpty().And.Equal(existentes);
    }

    [Fact]
    public async Task Health_Quando_ApiSobe_Deve_RetornarSucesso()
    {
        using HttpResponseMessage resposta = await _api.Client.GetAsync(new Uri("/health", UriKind.Relative));

        resposta.IsSuccessStatusCode.Should().BeTrue();
    }
}
