using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Cartoes;

[ValueObject]
public readonly partial struct IdCartao
{
    private static Validation Validate(Guid input) => Id.Validar(input);
}

public sealed class Cartao : AggregateRoot<IdCartao>, IPertenceAoUsuario
{
    public IdUsuario IdUsuario { get; private set; }
    public Banco Banco { get; private set; } = null!;
    public SufixoCartao Sufixo { get; private set; } = null!;
    public DiaVencimentoCartao DiaVencimento { get; private set; } = null!;
    public DiaFechamentoCartao DiaFechamento { get; private set; } = null!;

    private Cartao()
    {
    }

    private Cartao(
        IdCartao id,
        IdUsuario idUsuario,
        Banco banco,
        SufixoCartao sufixo,
        DiaVencimentoCartao diaVencimento,
        DiaFechamentoCartao diaFechamento)
        : base(id)
    {
        IdUsuario = idUsuario;
        Banco = banco;
        Sufixo = sufixo;
        DiaVencimento = diaVencimento;
        DiaFechamento = diaFechamento;
    }

    /// <remarks>
    /// Interno pois o usuário não pode ter dois cartões do mesmo banco com o mesmo final, e só a
    /// <see cref="Services.CartaoFactory"/>, que recebe os cartões do usuário, confere isso.
    /// </remarks>
    internal static Cartao Criar(
        IdUsuario idUsuario,
        Banco banco,
        SufixoCartao sufixo,
        DiaVencimentoCartao diaVencimento,
        DiaFechamentoCartao diaFechamento)
    {
        return new Cartao(IdCartao.FromNewVersion7Guid(), idUsuario, banco, sufixo, diaVencimento, diaFechamento);
    }

    /// <remarks>
    /// As despesas já geradas não mudam, porque guardam a data projetada do pagamento: os dias novos valem para as
    /// geradas depois.
    /// </remarks>
    public void AlterarCicloFatura(DiaVencimentoCartao diaVencimento, DiaFechamentoCartao diaFechamento)
    {
        DiaVencimento = diaVencimento;
        DiaFechamento = diaFechamento;
    }
}
