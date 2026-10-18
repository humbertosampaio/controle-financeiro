using AutoFixture.Kernel;
using Microsoft.Extensions.Time.Testing;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

public class CurrentDateOnlyGenerator : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        FakeTimeProvider timeProvider = context.Create<FakeTimeProvider>();

        return typeof(DateOnly).Equals(request)
            ? DateOnly.FromDateTime(timeProvider.GetLocalNow().LocalDateTime)
            : new NoSpecimen();
    }
}
