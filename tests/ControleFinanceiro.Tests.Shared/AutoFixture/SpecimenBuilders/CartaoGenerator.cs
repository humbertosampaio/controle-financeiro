using AutoFixture.Kernel;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

/// <summary>
/// Monta o <see cref="Cartao"/> pela <see cref="CartaoFactory"/>, a única entrada, como o primeiro cartão
/// do usuário. Com <c>[Frozen] IdUsuario</c> no teste, o cartão sai desse usuário.
/// </summary>
public sealed class CartaoGenerator : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type t || t != typeof(Cartao))
            return new NoSpecimen();

        return CartaoFactory
            .Criar(
                context.Create<IdUsuario>(),
                [],
                context.Create<Banco>(),
                context.Create<SufixoCartao>(),
                context.Create<DiaVencimentoCartao>())
            .Value;
    }
}
