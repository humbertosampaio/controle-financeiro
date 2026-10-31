using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Cartoes;

public sealed class DiaVencimentoCartao : DiaValidoEmTodoMes
{
    private DiaVencimentoCartao(int value) : base(value)
    {
    }

    public static Result<DiaVencimentoCartao> Criar(int dia)
    {
        return Result.SuccessIf(
            EstaNoIntervalo(dia),
            value: new DiaVencimentoCartao(dia),
            $"O dia de vencimento do cartão deve estar entre {Minimo} e {Maximo}. Valor fornecido: {dia}");
    }

    public static explicit operator DiaVencimentoCartao(int dia) => Criar(dia).Value;
}
