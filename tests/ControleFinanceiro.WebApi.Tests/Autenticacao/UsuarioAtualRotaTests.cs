using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using ControleFinanceiro.WebApi.Autenticacao;
using Microsoft.AspNetCore.Http;

namespace ControleFinanceiro.WebApi.Tests.Autenticacao;

/// <summary>Testes de unidade, sem API nem banco: o adaptador lê o usuário da rota da requisição corrente.</summary>
public sealed class UsuarioAtualRotaTests
{
    [Theory, DomainAutoData]
    public void Id_Quando_RotaComIdUsuario_Deve_RetornarOId(IdUsuario esperado)
    {
        UsuarioAtualRota sut = CriarComRota(esperado.Value.ToString());

        IdUsuario result = sut.Id;

        result.Should().Be(esperado);
    }

    [Fact]
    public void Id_Quando_SemHttpContext_Deve_LancarExcecao()
    {
        UsuarioAtualRota sut = new(new HttpContextAccessor());

        Func<IdUsuario> ler = () => sut.Id;

        ler.Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public void Id_Quando_RotaSemIdUsuario_Deve_LancarExcecao()
    {
        UsuarioAtualRota sut = new(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });

        Func<IdUsuario> ler = () => sut.Id;

        ler.Should().ThrowExactly<InvalidOperationException>();
    }

    [Theory]
    [InlineData("nao-e-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Id_Quando_IdUsuarioMalformado_Deve_LancarExcecao(string valorRota)
    {
        UsuarioAtualRota sut = CriarComRota(valorRota);

        Func<IdUsuario> ler = () => sut.Id;

        ler.Should().ThrowExactly<InvalidOperationException>();
    }

    private static UsuarioAtualRota CriarComRota(string valorRota)
    {
        DefaultHttpContext httpContext = new();
        httpContext.Request.RouteValues[UsuarioAtualRota.ParametroRota] = valorRota;

        return new UsuarioAtualRota(new HttpContextAccessor { HttpContext = httpContext });
    }
}
