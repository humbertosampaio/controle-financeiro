namespace ControleFinanceiro.Mcp.Tests.Infraestrutura;

/// <summary>Toda classe de teste do servidor entra nesta collection e divide o mesmo container.</summary>
[CollectionDefinition(Nome)]
public sealed class ServidorMcpCollection : ICollectionFixture<ServidorMcpFixture>
{
    public const string Nome = "ServidorMcp";
}
