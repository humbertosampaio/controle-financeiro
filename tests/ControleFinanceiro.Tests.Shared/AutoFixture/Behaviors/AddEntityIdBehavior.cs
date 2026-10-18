using AutoFixture.Kernel;
using ControleFinanceiro.Tests.Shared.AutoFixture.Commands;
using ControleFinanceiro.Tests.Shared.AutoFixture.Specifications;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Behaviors;

public sealed class AddEntityIdBehavior : ISpecimenBuilderTransformation
{
    public ISpecimenBuilderNode Transform(ISpecimenBuilder builder) =>
        new Postprocessor(builder, new AddEntityIdCommand(), new EntityRequestSpecification());
}
