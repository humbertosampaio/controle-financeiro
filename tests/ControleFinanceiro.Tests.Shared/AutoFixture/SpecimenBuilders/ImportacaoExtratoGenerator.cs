using AutoFixture.Kernel;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

/// <summary>
/// Monta a <see cref="ImportacaoExtrato"/> pela <see cref="ImportacaoExtratoFactory"/>, a única entrada, a
/// partir de uma conta. Com <c>[Frozen] ContaBancaria</c> no teste, a importação sai dessa conta.
/// </summary>
public sealed class ImportacaoExtratoGenerator : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type t || t != typeof(ImportacaoExtrato))
            return new NoSpecimen();

        return ImportacaoExtratoFactory.Registrar(
            context.Create<ContaBancaria>(),
            context.Create<FormatoExtrato>(),
            context.Create<PeriodoExtrato>(),
            context.Create<byte>(),
            context.Create<DateTimeOffset>());
    }
}
