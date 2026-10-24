using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Cartoes;

public sealed class DiaVencimentoCartao : DiaValidoEmTodoMes
{
    /// <summary>
    /// O dia do cartão criado sem vencimento informado, como o que aparece pela primeira vez numa fatura.
    /// </summary>
    public static readonly DiaVencimentoCartao Padrao = new(Minimo);

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
