using AutoFixture.Kernel;
using CSharpFunctionalExtensions;
using System.Reflection;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Commands;

public sealed class AddEntityIdCommand : ISpecimenCommand
{
    public void Execute(object specimen, ISpecimenContext context)
    {
        PropertyInfo idProperty = specimen
            .GetType()
            .GetProperty(nameof(Entity<>.Id), BindingFlags.Public | BindingFlags.Instance)!;

        object id = context.Resolve(idProperty.PropertyType);
        idProperty.SetValue(specimen, id);
    }
}
