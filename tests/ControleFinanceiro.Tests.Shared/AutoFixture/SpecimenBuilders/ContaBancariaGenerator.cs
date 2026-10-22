using AutoFixture.Kernel;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

/// <summary>
/// Monta a <see cref="ContaBancaria"/> pela <see cref="ContaBancariaFactory"/>, a única entrada, como a
/// primeira conta do usuário. Com <c>[Frozen] IdUsuario</c> no teste, a conta sai desse usuário.
/// </summary>
public sealed class ContaBancariaGenerator : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type t || t != typeof(ContaBancaria))
            return new NoSpecimen();

        return ContaBancariaFactory
            .ObterOuCriar(
                context.Create<IdUsuario>(),
                contasDoUsuario: [],
                context.Create<Banco>(),
                context.Create<TipoConta>(),
                context.Create<SufixoConta>())
            .Value;
    }
}
