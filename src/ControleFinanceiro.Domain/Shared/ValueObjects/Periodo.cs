namespace ControleFinanceiro.Domain.Shared.ValueObjects;

/// <summary>
/// Um intervalo de dias, com o início e o fim inclusos, como os dias que um arquivo importado cobre ou os de um mês. No
/// extrato, é o período que o cabeçalho declara, e o fim pode ser o dia da exportação, ainda incompleto.
/// </summary>
public sealed class Periodo : ValueObject
{
    private const string ErroInicioDepoisFim = "O início do período não pode ser depois do fim.";

    public DateOnly Inicio { get; }
    public DateOnly Fim { get; }

    /// <summary>
    /// O período de datas sabidamente válidas, como as de um teste ou as que o próprio código calculou. A entrada que pode
    /// ser inválida, como o cabeçalho de um arquivo, passa pelo <see cref="Criar"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Quando o início é depois do fim: é bug de quem chama.</exception>
    public Periodo(DateOnly inicio, DateOnly fim)
    {
        if (inicio > fim)
            throw new ArgumentException(ErroInicioDepoisFim, nameof(fim));

        Inicio = inicio;
        Fim = fim;
    }

    /// <summary>O período, ou a falha quando o início é depois do fim.</summary>
    public static Result<Periodo> Criar(DateOnly inicio, DateOnly fim)
    {
        if (inicio > fim)
            return Result.Failure<Periodo>(ErroInicioDepoisFim);

        return new Periodo(inicio, fim);
    }

    public bool Contem(DateOnly data) => data >= Inicio && data <= Fim;

    /// <summary>O mesmo período, começando <paramref name="diasAntes"/> dias antes e terminando <paramref name="diasDepois"/> dias depois.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Quando algum dos dias é negativo: é bug de quem chama.</exception>
    public Periodo Ampliado(int diasAntes, int diasDepois)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(diasAntes);
        ArgumentOutOfRangeException.ThrowIfNegative(diasDepois);

        return new Periodo(Inicio.AddDays(-diasAntes), Fim.AddDays(diasDepois));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Inicio;
        yield return Fim;
    }
}
