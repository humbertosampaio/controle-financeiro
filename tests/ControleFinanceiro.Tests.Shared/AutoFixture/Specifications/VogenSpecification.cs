using AutoFixture.Kernel;
using System.Diagnostics.CodeAnalysis;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Specifications;

public sealed class VogenSpecification : IRequestSpecification
{
    private static readonly Type _vogenGenericDefinition = typeof(IVogen<,>);

    public Type? VogenInterface { get; private set; }
    public Type? PrimitiveType { get; private set; }

    [MemberNotNullWhen(true, nameof(VogenInterface))]
    [MemberNotNullWhen(true, nameof(PrimitiveType))]
    public bool IsSatisfiedBy(object request)
    {
        if (request is Type type
            && TryGetVogenInterface(type, out Type? vogenInterface))
        {
            VogenInterface = vogenInterface;
            PrimitiveType = VogenInterface.GetGenericArguments()[1];
            return true;
        }

        return false;
    }

    private static bool TryGetVogenInterface(Type request, [NotNullWhen(true)] out Type? vogenInterface)
    {
        vogenInterface = request
            .GetInterfaces()
            .FirstOrDefault(i =>
                i.IsGenericType
                && i.GetGenericTypeDefinition() == _vogenGenericDefinition);

        return vogenInterface is not null;
    }
}