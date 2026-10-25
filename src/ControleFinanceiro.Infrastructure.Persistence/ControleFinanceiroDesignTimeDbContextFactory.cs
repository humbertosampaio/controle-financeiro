using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ControleFinanceiro.Infrastructure.Persistence;

/// <summary>
/// Usada por <c>dotnet ef</c> (projeto Infrastructure, startup WebApi).
/// Sem <c>ConnectionStrings__controlefinanceiro</c>, usa uma conexão sem credenciais: basta para
/// <c>migrations add</c>, que não abre conexão. O banco é migrado quando a WebApi sobe em Development.
/// </summary>
public sealed class ControleFinanceiroDesignTimeDbContextFactory : IDesignTimeDbContextFactory<ControleFinanceiroDbContext>
{
    public ControleFinanceiroDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__controlefinanceiro")
            ?? "Host=localhost;Port=5432;Database=controlefinanceiro";

        DbContextOptionsBuilder<ControleFinanceiroDbContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql(connectionString);

        return new ControleFinanceiroDbContext(optionsBuilder.Options, new UsuarioAtualIndisponivel());
    }
}
