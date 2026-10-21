namespace ControleFinanceiro.Domain.Importacoes;

/// <summary>
/// Os dias que o extrato cobre, como o cabeçalho do arquivo declara, com o início e o fim inclusos. O fim
/// pode ser o dia da exportação, ainda incompleto.
/// </summary>
public sealed class PeriodoExtrato : ValueObject
{
    public DateOnly Inicio { get; }
    public DateOnly Fim { get; }

    private PeriodoExtrato(DateOnly inicio, DateOnly fim)
    {
        Inicio = inicio;
        Fim = fim;
    }

    public static Result<PeriodoExtrato> Criar(DateOnly inicio, DateOnly fim)
    {
        return Result.SuccessIf(
            inicio <= fim,
            new PeriodoExtrato(inicio, fim),
            "O início do período do extrato não pode ser depois do fim.");
    }

    public bool Contem(DateOnly data) => data >= Inicio && data <= Fim;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Inicio;
        yield return Fim;
    }
}
