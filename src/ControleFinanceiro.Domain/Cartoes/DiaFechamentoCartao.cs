using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Cartoes;

/// <summary>O último dia de compras da fatura do cartão.</summary>
public sealed class DiaFechamentoCartao : DiaValidoEmTodoMes
{
    private DiaFechamentoCartao(int value) : base(value)
    {
    }

    public static Result<DiaFechamentoCartao> Criar(int dia)
    {
        return Result.SuccessIf(
            EstaNoIntervalo(dia),
            value: new DiaFechamentoCartao(dia),
            $"O dia de fechamento do cartão deve estar entre {Minimo} e {Maximo}. Valor fornecido: {dia}");
    }

    public static explicit operator DiaFechamentoCartao(int dia) => Criar(dia).Value;
}
