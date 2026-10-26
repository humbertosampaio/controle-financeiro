using AutoFixture.Kernel;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

/// <summary>
/// Monta a <see cref="CategoriaDespesa"/> pela <see cref="CategoriaDespesaFactory"/>, a única entrada, como a primeira
/// categoria do usuário, com um alias. Com <c>[Frozen] IdUsuario</c> ou <c>[Frozen] Alias</c> no teste, a categoria
/// sai desse usuário ou com esse alias.
/// </summary>
public sealed class CategoriaDespesaGenerator : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type t || t != typeof(CategoriaDespesa))
            return new NoSpecimen();

        CategoriaNova nova = new(context.Create<TextoObrigatorio>(), context.Create<Alias>());

        return CategoriaDespesaFactory
            .Resolver(context.Create<IdUsuario>(), categoriasDoUsuario: [], [nova])
            .Value[0];
    }
}
