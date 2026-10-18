using AutoFixture.Kernel;
using System.Reflection;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

public sealed class AsyncEnumerableRelay : ISpecimenBuilder
{
    public object? Create(object request, ISpecimenContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (request is not Type type)
            return new NoSpecimen();

        if (!type.TryGetSingleGenericTypeArgument(typeof(IAsyncEnumerable<>), out Type? enumerableType))
            return new NoSpecimen();

        object specimen = context.Resolve(new MultipleRequest(enumerableType));
        if (specimen is OmitSpecimen)
            return specimen;

        if (specimen is not IEnumerable<object> enumerable)
            return new NoSpecimen();

        Type typedAdapterType = typeof(ConvertedAsyncEnumerable<>).MakeGenericType(enumerableType!);
        return Activator.CreateInstance(typedAdapterType, enumerable.ToAsyncEnumerable());
    }

    internal class ConvertedAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<object> _enumerable;

        public ConvertedAsyncEnumerable(IAsyncEnumerable<object> enumerable)
        {
            _enumerable = enumerable ?? throw new ArgumentNullException(nameof(enumerable));
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            await foreach (object item in _enumerable)
            {
                if (item is T variable)
                    yield return variable;
            }
        }
    }
}

file static class Extensions
{
    extension(Type type)
    {
        public bool TryGetSingleGenericTypeArgument(Type expectedGenericDefinition, out Type? argument)
        {
            if (!expectedGenericDefinition.GetTypeInfo().IsGenericTypeDefinition)
                throw new ArgumentException("Must be a generic type definition", nameof(expectedGenericDefinition));

            TypeInfo typeInfo = type.GetTypeInfo();
            if (typeInfo.IsGenericType && type.GetGenericTypeDefinition() == expectedGenericDefinition)
            {
                Type[] typeArguments = typeInfo.GenericTypeArguments;
                if (typeArguments.Length == 1)
                {
                    argument = typeArguments[0];
                    return true;
                }
            }

            argument = null;
            return false;
        }
    }
}
