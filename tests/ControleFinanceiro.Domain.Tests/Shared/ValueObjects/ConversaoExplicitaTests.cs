using ControleFinanceiro.Domain.Shared.ValueObjects;
using CSharpFunctionalExtensions;
using System.Reflection;

namespace ControleFinanceiro.Domain.Tests.Shared.ValueObjects;

/// <summary>
/// Todo <see cref="SimpleValueObject{T}"/> concreto do domínio declara o <c>explicit operator</c> do primitivo, para o
/// teste, o generator e a conversão EF montarem o valor sem o <c>Criar</c> (padrões de domínio, §7). Ficam de fora o
/// tipo abstrato e as exceções listadas, que declaram a conversão implícita, como o <see cref="Reais"/>: exceção nova
/// entra na lista e na §7.
/// </summary>
public sealed class ConversaoExplicitaTests
{
    private static readonly Type[] _valueObjectsConversaoImplicita = [typeof(Reais)];

    [Fact]
    public void SimpleValueObjectsConcretos_Deve_DeclararConversaoExplicitaPrimitivo()
    {
        IEnumerable<Type> simpleValueObjects = typeof(DomainAssemblyMarker).Assembly.GetTypes()
            .Where(tipo => !tipo.IsAbstract && !_valueObjectsConversaoImplicita.Contains(tipo))
            .Where(tipo => PrimitivoSimpleValueObject(tipo) is not null);

        simpleValueObjects.Should().NotBeEmpty()
            .And.OnlyContain(tipo => DeclaraConversaoExplicita(tipo));
    }

    private static Type? PrimitivoSimpleValueObject(Type tipo)
    {
        for (Type? atual = tipo.BaseType; atual is not null; atual = atual.BaseType)
        {
            if (atual.IsGenericType && atual.GetGenericTypeDefinition() == typeof(SimpleValueObject<>))
            {
                return atual.GetGenericArguments()[0];
            }
        }

        return null;
    }

    private static bool DeclaraConversaoExplicita(Type tipo)
    {
        Type primitivo = PrimitivoSimpleValueObject(tipo)!;

        return tipo.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Any(metodo => metodo.Name == "op_Explicit"
                && metodo.ReturnType == tipo
                && metodo.GetParameters() is [ParameterInfo parametro]
                && parametro.ParameterType == primitivo);
    }
}
