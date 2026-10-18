using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Shared.ValueObjects;

public sealed class DiaCompraRecorrente : SimpleValueObject<int>
{
    private DiaCompraRecorrente(int value) : base(value)
    {
    }

    public static Result<DiaCompraRecorrente> Criar(int dia)
    {
        return Result.SuccessIf(
            dia is >= 1 and <= 28,
            value: new DiaCompraRecorrente(dia),
            $"O dia da compra de uma compra recorrente deve estar entre 1 e 28. Valor fornecido: {dia}");
    }

    public static explicit operator DiaCompraRecorrente(int dia) => Criar(dia).Value;
}