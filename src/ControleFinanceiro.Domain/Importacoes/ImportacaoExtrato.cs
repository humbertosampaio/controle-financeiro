using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Domain.Importacoes;

[ValueObject]
public readonly partial struct IdImportacao
{
    private static Validation Validate(Guid input) => Id.Validar(input);
}

/// <summary>
/// O registro de um arquivo importado: de qual origem, em qual formato, que período cobria e quantas
/// transações eram novas. É por ele que a importação seguinte descobre se ficou um buraco entre as duas.
/// </summary>
public sealed class ImportacaoExtrato : AggregateRoot<IdImportacao>
{
    public OrigemTransacao Origem { get; private set; } = null!;
    public FormatoExtrato Formato { get; private set; } = null!;
    public PeriodoExtrato Periodo { get; private set; } = null!;
    public int Lidas { get; private set; }
    public int Novas { get; private set; }
    public int JaExistentes { get; private set; }
    public DateTimeOffset ImportadaEm { get; private set; }

    private ImportacaoExtrato()
    {
    }

    private ImportacaoExtrato(
        IdImportacao id,
        OrigemTransacao origem,
        FormatoExtrato formato,
        PeriodoExtrato periodo,
        int lidas,
        int novas,
        DateTimeOffset importadaEm)
            : base(id)
    {
        Origem = origem;
        Formato = formato;
        Periodo = periodo;
        Lidas = lidas;
        Novas = novas;
        JaExistentes = lidas - novas;
        ImportadaEm = importadaEm;
    }

    /// <param name="lidas">Quantos lançamentos o arquivo trazia.</param>
    /// <param name="novas">Quantas transações foram adicionadas; as demais já estavam no banco de dados.</param>
    public static ImportacaoExtrato Registrar(
        OrigemTransacao origem,
        FormatoExtrato formato,
        PeriodoExtrato periodo,
        int lidas,
        int novas,
        DateTimeOffset importadaEm)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(novas);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(novas, lidas);
        return new(IdImportacao.FromNewVersion7Guid(), origem, formato, periodo, lidas, novas, importadaEm);
    }

    /// <summary>
    /// Os dias entre o fim da <paramref name="ultima"/> importação da origem e o início desta, que nenhum
    /// arquivo cobriu. Nada quando é a primeira importação ou quando os períodos se tocam ou se sobrepõem.
    /// </summary>
    /// <param name="ultima">A importação mais recente da mesma origem, antes desta.</param>
    public Maybe<PeriodoExtrato> PeriodoNaoImportado(Maybe<ImportacaoExtrato> ultima)
    {
        if (ultima.HasNoValue)
            return Maybe.None;

        if (ultima.Value.Origem != Origem)
            throw new ArgumentException("A importação anterior é de outra origem.", nameof(ultima));

        DateOnly primeiroDiaNaoImportado = ultima.Value.Periodo.Fim.AddDays(1);
        if (Periodo.Inicio <= primeiroDiaNaoImportado)
            return Maybe.None;

        return PeriodoExtrato.Criar(primeiroDiaNaoImportado, Periodo.Inicio.AddDays(-1)).Value;
    }
}
