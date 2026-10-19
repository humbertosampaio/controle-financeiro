namespace ControleFinanceiro.WebApi.Tests.Infraestrutura;

/// <summary>
/// Toda classe de teste de integração entra nesta collection e divide o mesmo container.
/// Por isso as classes rodam em sequência, sem disputar o banco.
/// </summary>
[CollectionDefinition(Nome)]
public sealed class ApiCollection : ICollectionFixture<ApiComPostgresFixture>
{
    public const string Nome = "Api";
}
