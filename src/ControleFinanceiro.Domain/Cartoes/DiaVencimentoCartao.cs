namespace ControleFinanceiro.Domain.Cartoes;

public sealed class DiaVencimentoCartao : SimpleValueObject<int>
{
    private DiaVencimentoCartao(int value) : base(value)
    {
    }

    public static Result<DiaVencimentoCartao> Criar(int dia)
    {
        return Result.SuccessIf(
            dia is >= 1 and <= 28,
            value: new DiaVencimentoCartao(dia),
            $"O dia de vencimento do cartão deve estar entre 1 e 28. Valor fornecido: {dia}");
    }

    public static explicit operator DiaVencimentoCartao(int dia) => Criar(dia).Value;
}
