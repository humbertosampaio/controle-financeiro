using ArchUnitNET.xUnit;
using ControleFinanceiro.Application;
using ControleFinanceiro.Domain;
using System.Reflection;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using Architecture = ArchUnitNET.Domain.Architecture;
using ArchLoader = ArchUnitNET.Loader.ArchLoader;

namespace ControleFinanceiro.SaidaModelo.Tests.Arquitetura;

/// <summary>
/// O domínio não conhece infraestrutura: nenhum tipo do Domain usa EF Core, ASP.NET nem Npgsql (inviolável 4 do
/// <c>CLAUDE.md</c> da raiz), nem tipo de outro projeto da solução. A Application também não usa infraestrutura, e dos
/// projetos da solução usa só o Domain. Conta o uso no código: uma referência declarada no <c>.csproj</c> e sem uso
/// não é conferida.
/// </summary>
public sealed class DependenciasCamadasTests
{
    private static readonly Assembly _application = typeof(ApplicationAssemblyMarker).Assembly;
    private static readonly Assembly _domain = typeof(DomainAssemblyMarker).Assembly;

    private static readonly Architecture _arquitetura = new ArchLoader()
        .LoadAssembliesIncludingDependencies(_application, _domain)
        .Build();

    private const string NamespacesInfraestrutura = @"^(Microsoft\.EntityFrameworkCore|Microsoft\.AspNetCore|Npgsql)(\..*)?$";

    [Fact]
    public void Domain_Deve_NaoUsarInfraestrutura()
    {
        Types().That().ResideInAssembly(_domain)
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(NamespacesInfraestrutura))
            .Check(_arquitetura);
    }

    [Fact]
    public void Domain_Deve_NaoUsarOutroProjetoSolucao()
    {
        Types().That().ResideInAssembly(_domain)
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(@"^ControleFinanceiro\.")
                .And().DoNotResideInAssembly(_domain))
            .Check(_arquitetura);
    }

    [Fact]
    public void Application_Deve_NaoUsarInfraestrutura()
    {
        Types().That().ResideInAssembly(_application)
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(NamespacesInfraestrutura))
            .Check(_arquitetura);
    }

    [Fact]
    public void Application_Deve_UsarApenasDomainEntreProjetosSolucao()
    {
        Types().That().ResideInAssembly(_application)
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(@"^ControleFinanceiro\.")
                .And().DoNotResideInAssembly(_application)
                .And().DoNotResideInAssembly(_domain))
            .Check(_arquitetura);
    }
}
