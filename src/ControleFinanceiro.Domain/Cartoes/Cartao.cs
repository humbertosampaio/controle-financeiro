using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Cartoes;

[ValueObject]
public readonly partial struct IdCartao
{
    private static Validation Validate(Guid input) => Id.Validar(input);
}

public sealed class Cartao : AggregateRoot<IdCartao>
{
    public IdUsuario IdUsuario { get; private set; }
    public Banco Banco { get; private set; } = null!;
    public SufixoCartao Sufixo { get; private set; } = null!;
    public DiaVencimentoCartao DiaVencimento { get; private set; } = null!;

    private Cartao()
    {
    }

    private Cartao(IdCartao id, IdUsuario idUsuario, Banco banco, SufixoCartao sufixo, DiaVencimentoCartao diaVencimento)
        : base(id)
    {
        IdUsuario = idUsuario;
        Banco = banco;
        Sufixo = sufixo;
        DiaVencimento = diaVencimento;
    }

    public static Cartao Criar(IdUsuario idUsuario, Banco banco, SufixoCartao sufixo, DiaVencimentoCartao diaVencimento)
    {
        return new Cartao(IdCartao.FromNewVersion7Guid(), idUsuario, banco, sufixo, diaVencimento);
    }
}
