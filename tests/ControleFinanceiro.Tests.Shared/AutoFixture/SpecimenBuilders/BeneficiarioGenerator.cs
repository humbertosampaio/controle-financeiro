using AutoFixture.Kernel;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

/// <summary>
/// Monta o <see cref="Beneficiario"/> pela <see cref="BeneficiarioFactory"/>, a única entrada, como o primeiro
/// beneficiário do usuário, com um alias. Com <c>[Frozen] IdUsuario</c> ou <c>[Frozen] Alias</c> no teste, o
/// beneficiário sai desse usuário ou com esse alias.
/// </summary>
public sealed class BeneficiarioGenerator : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type t || t != typeof(Beneficiario))
            return new NoSpecimen();

        return BeneficiarioFactory
            .Criar(
                context.Create<IdUsuario>(),
                beneficiariosDoUsuario: [],
                context.Create<TextoObrigatorio>(),
                context.Create<Alias>())
            .Value;
    }
}
