using AutoFixture.Kernel;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

/// <summary>
/// Monta a <see cref="ImportacaoExtrato"/> com contagens coerentes: as novas nunca passam das lidas.
/// </summary>
public sealed class ImportacaoExtratoGenerator : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type t || t != typeof(ImportacaoExtrato))
            return new NoSpecimen();

        OrigemTransacao origem = context.Create<OrigemTransacao>();
        FormatoExtrato formato = context.Create<FormatoExtrato>();
        PeriodoExtrato periodo = context.Create<PeriodoExtrato>();
        byte novas = context.Create<byte>();
        byte jaExistentes = context.Create<byte>();
        DateTimeOffset importadaEm = context.Create<DateTimeOffset>();

        return ImportacaoExtrato.Registrar(origem, formato, periodo, novas + jaExistentes, novas, importadaEm);
    }
}
