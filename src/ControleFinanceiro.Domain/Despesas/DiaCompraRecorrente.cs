using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Despesas;

public sealed class DiaCompraRecorrente : DiaValidoEmTodoMes
{
    private DiaCompraRecorrente(int value) : base(value)
    {
    }

    public static Result<DiaCompraRecorrente> Criar(int dia)
    {
        return Result.SuccessIf(
            EstaNoIntervalo(dia),
            value: new DiaCompraRecorrente(dia),
            $"O dia da compra de uma compra recorrente deve estar entre {Minimo} e {Maximo}. Valor fornecido: {dia}");
    }

    public static explicit operator DiaCompraRecorrente(int dia) => Criar(dia).Value;
}