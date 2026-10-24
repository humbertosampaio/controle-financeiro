namespace ControleFinanceiro.Domain.Transacoes;

/// <summary>
/// A parcela de uma compra parcelada, como o banco a informou no lançamento (<c>3/10</c>). A compra à vista
/// não tem parcela: quem guarda uma <see cref="Parcela"/> a declara anulável. O total é de pelo menos 2,
/// porque a compra numa parcela só é adiada, e não parcelada.
/// </summary>
/// <remarks>
/// Não é a <see cref="Despesas.ParcelaDespesa"/>, que é a parcela que o usuário espera pagar: esta é o que veio
/// no arquivo.
/// </remarks>
public sealed class Parcela : ValueObject
{
    public const int TotalMinimo = 2;

    public int Numero { get; }
    public int Total { get; }

    private Parcela(int numero, int total)
    {
        Numero = numero;
        Total = total;
    }

    public static Result<Parcela> Criar(int numero, int total)
    {
        return Result.SuccessIf(
            total >= TotalMinimo && numero >= 1 && numero <= total,
            value: new Parcela(numero, total),
            $"A parcela deve ter o total a partir de {TotalMinimo} e o número entre 1 e o total. Valores fornecidos: {numero} de {total}");
    }

    public static explicit operator Parcela((int Numero, int Total) parcela) => Criar(parcela.Numero, parcela.Total).Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Numero;
        yield return Total;
    }
}
