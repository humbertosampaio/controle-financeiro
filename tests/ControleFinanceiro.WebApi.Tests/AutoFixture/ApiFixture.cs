using ControleFinanceiro.Tests.Shared.AutoFixture;

namespace ControleFinanceiro.WebApi.Tests.AutoFixture;

/// <summary>
/// O <see cref="DomainFixture"/> com o que os testes de integração acrescentam.
/// </summary>
public sealed class ApiFixture : DomainFixture
{
    public static new ApiFixture Create() => new();
}
