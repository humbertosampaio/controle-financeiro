using AutoFixture.Kernel;
using CSharpFunctionalExtensions;
using System.Reflection;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Specifications;

public sealed class EntityRequestSpecification : IRequestSpecification
{
    private static readonly Type _openEntityType = typeof(Entity<>);

    public bool IsSatisfiedBy(object request)
    {
        if (request is not Type requestType)
            return false;

        PropertyInfo? idProperty = requestType.GetProperty(nameof(Entity<>.Id), BindingFlags.Public | BindingFlags.Instance);
        if (idProperty is null)
            return false;

        Type genericEntityType = _openEntityType.MakeGenericType(idProperty.PropertyType);
        if (!requestType.IsAssignableTo(genericEntityType))
            return false;

        return true;
    }
}
