using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Cartoes;

/// <summary>
/// O dia de vencimento da fatura do cartão, de 1 a 24. O banco empurra o vencimento que cai em fim de semana ou feriado
/// por até 4 dias, e com o máximo no dia 24 o vencimento empurrado nunca muda de mês, nem em fevereiro: o mês da fatura
/// sai da data do vencimento.
/// </summary>
public sealed class DiaVencimentoCartao : DiaValidoEmTodoMes
{
    public new const int Maximo = 24;

    private DiaVencimentoCartao(int value) : base(value)
    {
    }

    public static Result<DiaVencimentoCartao> Criar(int dia)
    {
        return Result.SuccessIf(
            dia is >= Minimo and <= Maximo,
            value: new DiaVencimentoCartao(dia),
            $"O dia de vencimento do cartão deve estar entre {Minimo} e {Maximo}. Valor fornecido: {dia}");
    }

    public static explicit operator DiaVencimentoCartao(int dia) => Criar(dia).Value;
}
