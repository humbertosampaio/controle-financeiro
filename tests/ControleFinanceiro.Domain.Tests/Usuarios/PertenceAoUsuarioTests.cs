using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Tests.Usuarios;

public sealed class PertenceAoUsuarioTests
{
    [Fact]
    public void RaizesDeAgregado_Quando_TemIdUsuario_Deve_ImplementarPertenceAoUsuario()
    {
        IEnumerable<Type> raizesComIdUsuario = typeof(Usuario).Assembly.GetTypes()
            .Where(EhRaizDeAgregado)
            .Where(tipo => tipo.GetProperty(nameof(IPertenceAoUsuario.IdUsuario))?.PropertyType == typeof(IdUsuario));

        raizesComIdUsuario.Should().NotBeEmpty()
            .And.OnlyContain(tipo => typeof(IPertenceAoUsuario).IsAssignableFrom(tipo));
    }

    [Fact]
    public void Usuario_Deve_NaoImplementarPertenceAoUsuario()
    {
        typeof(Usuario).Should().NotImplement<IPertenceAoUsuario>();
    }

    private static bool EhRaizDeAgregado(Type tipo)
    {
        for (Type? atual = tipo.BaseType; atual is not null; atual = atual.BaseType)
        {
            if (atual.IsGenericType && atual.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
            {
                return true;
            }
        }

        return false;
    }
}
