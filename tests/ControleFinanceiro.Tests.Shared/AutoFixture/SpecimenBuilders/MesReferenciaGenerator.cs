using AutoFixture.Kernel;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

public sealed class MesReferenciaGenerator : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        return request switch
        {
            Type t when t == typeof(MesReferencia) => (MesReferencia)context.Create<DateTimeOffset>(),
            _ => new NoSpecimen()
        };
    }
}