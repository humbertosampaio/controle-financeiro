using ControleFinanceiro.Tests.Shared.AutoFixture.Behaviors;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Customizations;

/// <summary>
/// Aplica um Id aleatório a toda instância que herda de <see cref="Entity{Tid}"/>
/// </summary>
internal sealed class EntityIdCustomization : ICustomization
{
    public void Customize(IFixture fixture) =>
        fixture.Behaviors.Add(new AddEntityIdBehavior());
}
