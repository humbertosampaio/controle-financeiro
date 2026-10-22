using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Cartoes;

[ValueObject]
public readonly partial struct IdCartao
{
    private static Validation Validate(Guid input) => Id.Validar(input);
}

public sealed class Cartao : Entity<IdCartao>
{
    public Banco Banco { get; private set; } = null!;
    public SufixoCartao Sufixo { get; private set; } = null!;
    public DiaVencimentoCartao DiaVencimento { get; private set; } = null!;

    private Cartao()
    {
    }

    private Cartao(IdCartao id, Banco banco, SufixoCartao sufixo, DiaVencimentoCartao diaVencimento)
        : base(id)
    {
        Banco = banco;
        Sufixo = sufixo;
        DiaVencimento = diaVencimento;
    }

    public static Cartao Criar(Banco banco, SufixoCartao sufixo, DiaVencimentoCartao diaVencimento)
    {
        return new Cartao(IdCartao.FromNewVersion7Guid(), banco, sufixo, diaVencimento);
    }
}
