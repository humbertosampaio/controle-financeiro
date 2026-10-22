using AutoFixture.Kernel;
using ControleFinanceiro.Tests.Shared.AutoFixture.Specifications;
using System.Reflection;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

public sealed class VogenSpecimenBuilder : ISpecimenBuilder
{
    private readonly VogenSpecification _vogenSpecification;

    public VogenSpecimenBuilder(VogenSpecification vogenSpecification)
    {
        _vogenSpecification = vogenSpecification;
    }

    public object Create(object request, ISpecimenContext context)
    {
        if (!_vogenSpecification.IsSatisfiedBy(request))
            return new NoSpecimen();

        Type requestAsType = (Type)request;
        object primitiveId = context.Resolve(_vogenSpecification.PrimitiveType);
        MethodInfo vogenFromMethod = requestAsType.GetMethod(nameof(IVogen<,>.From), BindingFlags.Public | BindingFlags.Static)!;

        return vogenFromMethod.Invoke(obj: null, [primitiveId])
            ?? new NoSpecimen();
    }
}