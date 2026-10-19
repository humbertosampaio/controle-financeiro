using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Usuarios;

public sealed class Cartao : ComparableValueObject
{
    public SufixoCartao Sufixo { get; private set; } = null!;
    public DiaVencimentoCartao DiaVencimento { get; private set; } = null!;

    private Cartao()
    {
    }

    public Cartao(SufixoCartao sufixo, DiaVencimentoCartao diaVencimento)
    {
        Sufixo = sufixo;
        DiaVencimento = diaVencimento;
    }

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return Sufixo;
        yield return DiaVencimento;
    }
}