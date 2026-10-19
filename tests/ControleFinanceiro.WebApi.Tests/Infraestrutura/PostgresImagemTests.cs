using ControleFinanceiro.AppHost.Resources;

namespace ControleFinanceiro.WebApi.Tests.Infraestrutura;

public sealed class PostgresImagemTests
{
    [Fact]
    public void Completa_Quando_ComparadaAoDockerfile_Deve_SerAImagemDoFrom()
    {
        // Num PR do Dependabot que atualiza o Dockerfile, este teste falha e aponta a constante a atualizar.
        string dockerfile = Path.Combine(AppContext.BaseDirectory, "Infraestrutura", "postgres.Dockerfile");
        string from = File.ReadLines(dockerfile)
            .Select(linha => linha.Trim())
            .Single(linha => linha.StartsWith("FROM ", StringComparison.Ordinal));

        from["FROM ".Length..].Trim().Should().Be(
            PostgresImagem.Completa,
            "PostgresImagem.cs precisa acompanhar o FROM de src/ControleFinanceiro.AppHost/Resources/postgres.Dockerfile");
    }
}
