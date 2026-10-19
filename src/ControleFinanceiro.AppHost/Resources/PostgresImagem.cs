namespace ControleFinanceiro.AppHost.Resources;

/// <summary>
/// Imagem do Postgres usada pelo AppHost e pelos testes de integração. A mesma referência está em
/// <c>postgres.Dockerfile</c>, que o Dependabot observa; um teste confere que as duas batem.
/// </summary>
internal static class PostgresImagem
{
    internal const string Registro = "docker.io";
    internal const string Imagem = "library/postgres";
    internal const string Tag = "18.3";

    internal const string Completa = $"{Registro}/{Imagem}:{Tag}";
}
