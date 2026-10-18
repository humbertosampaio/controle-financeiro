using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Shared.ValueObjects;

public sealed class MesReferencia : ComparableValueObject
{
    public int Mes { get; }
    public int Ano { get; }
    public DateOnly Inicio { get; }
    public DateOnly Fim { get; }

    private MesReferencia(int mes, int ano)
    {
        Mes = mes;
        Ano = ano;
        Inicio = new DateOnly(ano, mes, 1);
        Fim = new DateOnly(ano, mes, DateTime.DaysInMonth(ano, mes));
    }

    public static Result<MesReferencia> Criar(int mes, int ano)
    {
        if (mes is < 1 or > 12)
            return Result.Failure<MesReferencia>($"Mês deve estar entre 1 e 12. Valor fornecido: {mes}");

        if (ano < 0)
            return Result.Failure<MesReferencia>($"Ano não pode ser negativo. Valor fornecido: {ano}");

        return new MesReferencia(mes, ano);
    }

    public MesReferencia Anterior() => Inicio.AddMonths(-1);
    public MesReferencia Posterior() => Inicio.AddMonths(1);

    /// <summary>
    /// Tenta criar um <see cref="DateOnly"/> representando o dia específico dentro do mês de referência. 
    /// Caso o dia seja inválido para o mês de referência atual, retorna uma falha.
    /// </summary>
    /// <param name="dia">O componente dia da data</param>
    public Result<DateOnly> NoDia(int dia)
    {
        return Result.Try(
            () => new DateOnly(Ano, Mes, dia),
            _ => $"O dia {dia} é inválido para o mês de referência atual '{this}'");
    }

    /// <summary>
    /// Cria um <see cref="DateOnly"/> representando o dia específico dentro do mês de referência. 
    /// </summary>
    /// <param name="dia">O componente dia da data</param>
    public DateOnly NoDia(DiaVencimentoCartao dia) => new(Ano, Mes, dia);

    /// <summary>
    /// Cria um <see cref="DateOnly"/> representando o dia específico dentro do mês de referência. 
    /// </summary>
    /// <param name="dia">O componente dia da data</param>
    public DateOnly NoDia(DiaCompraRecorrente dia) => new(Ano, Mes, dia);

    public bool Contem(DateOnly data) => data >= Inicio && data <= Fim;
    public bool Contem(DateTimeOffset data) => Contem(data.ToDateOnly());

    public override string ToString() => $"{Mes:00}/{Ano}";

    public static bool operator >(MesReferencia a, MesReferencia b) => a.Inicio > b.Inicio;
    public static bool operator <(MesReferencia a, MesReferencia b) => a.Inicio < b.Inicio;

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return Inicio;
    }

    public static implicit operator MesReferencia(DateTimeOffset referencia) => new(referencia.Month, referencia.Year);
    public static implicit operator MesReferencia(DateOnly referencia) => new(referencia.Month, referencia.Year);
}
