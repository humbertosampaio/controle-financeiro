using ControleFinanceiro.WebApi.Autenticacao;
using ControleFinanceiro.WebApi.Endpoints.Usuarios;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using FastEndpoints;
using Microsoft.AspNetCore.Routing;

namespace ControleFinanceiro.WebApi.Tests.Autenticacao;

/// <summary>
/// Confere a convenção na API composta pelo <c>Program.cs</c>: todo endpoint com <c>{idUsuario}</c> na rota passa pelo
/// <see cref="UsuarioRotaPreProcessor"/>. O 404 em si é conferido nos testes de usuário inexistente de cada endpoint.
/// </summary>
[Collection(ApiCollection.Nome)]
public sealed class UsuarioRotaPreProcessorTests
{
    private readonly ApiComPostgresFixture _api;

    public UsuarioRotaPreProcessorTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    [Fact]
    public void Aplicar_Deve_LigarOPreProcessorEmTodoEndpointComIdUsuarioNaRota()
    {
        List<RouteEndpoint> comIdUsuario = EndpointsDaApi()
            .Where(e => e.RoutePattern.GetParameter(UsuarioAtualRota.ParametroRota) is not null)
            .ToList();

        comIdUsuario.Should().NotBeEmpty();
        comIdUsuario.Should().AllSatisfy(e => TemOPreProcessor(e).Should().BeTrue(e.DisplayName));
    }

    [Fact]
    public void Aplicar_Quando_RotaSemIdUsuario_Deve_NaoLigarOPreProcessor()
    {
        RouteEndpoint cadastroUsuario = EndpointsDaApi()
            .Single(e => e.Metadata.GetMetadata<EndpointDefinition>()!.EndpointType == typeof(CadastrarUsuarioEndpoint));

        cadastroUsuario.RoutePattern.GetParameter(UsuarioAtualRota.ParametroRota).Should().BeNull();

        TemOPreProcessor(cadastroUsuario).Should().BeFalse();
    }

    private IEnumerable<RouteEndpoint> EndpointsDaApi()
    {
        return _api.ObterServicos<EndpointDataSource>()
            .SelectMany(fonte => fonte.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(e => e.Metadata.GetMetadata<EndpointDefinition>() is not null);
    }

    private static bool TemOPreProcessor(RouteEndpoint endpoint)
    {
        return endpoint.Metadata.GetMetadata<EndpointDefinition>()!.PreProcessorsList.Any(p => p is UsuarioRotaPreProcessor);
    }
}
