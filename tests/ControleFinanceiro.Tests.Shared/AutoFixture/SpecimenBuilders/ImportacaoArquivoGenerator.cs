using AutoFixture.Kernel;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

/// <summary>
/// Monta a <see cref="ImportacaoArquivo"/> pela <see cref="ImportacaoArquivoFactory"/>, a única entrada, a
/// partir de uma conta. Com <c>[Frozen] ContaBancaria</c> no teste, a importação sai dessa conta.
/// </summary>
public sealed class ImportacaoArquivoGenerator : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type t || t != typeof(ImportacaoArquivo))
            return new NoSpecimen();

        return ImportacaoArquivoFactory.Registrar(
            context.Create<ContaBancaria>(),
            context.Create<LayoutArquivoImportacao>(),
            context.Create<PeriodoArquivo>(),
            context.Create<byte>(),
            context.Create<DateTimeOffset>());
    }
}
