namespace ControleFinanceiro.Domain.Importacoes;

/// <summary>
/// Os dias que um arquivo importado cobre, com o início e o fim inclusos. No extrato, é o período que o
/// cabeçalho declara, e o fim pode ser o dia da exportação, ainda incompleto.
/// </summary>
public sealed class PeriodoArquivo : ValueObject
{
    public DateOnly Inicio { get; }
    public DateOnly Fim { get; }

    private PeriodoArquivo(DateOnly inicio, DateOnly fim)
    {
        Inicio = inicio;
        Fim = fim;
    }

    public static Result<PeriodoArquivo> Criar(DateOnly inicio, DateOnly fim)
    {
        return Result.SuccessIf(
            inicio <= fim,
            new PeriodoArquivo(inicio, fim),
            "O início do período não pode ser depois do fim.");
    }

    public static explicit operator PeriodoArquivo((DateOnly Inicio, DateOnly Fim) periodo) => Criar(periodo.Inicio, periodo.Fim).Value;

    public bool Contem(DateOnly data) => data >= Inicio && data <= Fim;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Inicio;
        yield return Fim;
    }
}
