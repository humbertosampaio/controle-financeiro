using AutoFixture.Kernel;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

/// <summary>
/// Monta a <see cref="Parcela"/> válida: total de 2 a 24 e número de 1 ao total.
/// </summary>
public sealed class ParcelaGenerator : ISpecimenBuilder
{
    private const int TotalMaximo = 24;

    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type t || t != typeof(Parcela))
            return new NoSpecimen();

        int total = context.Create<byte>() % (TotalMaximo - Parcela.TotalMinimo + 1) + Parcela.TotalMinimo;
        int numero = context.Create<byte>() % total + 1;

        return (Parcela)(numero, total);
    }
}
