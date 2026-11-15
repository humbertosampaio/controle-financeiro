using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Cartoes;

/// <summary>
/// O vencimento de uma fatura: a data em que ela vence. Quando a fatura veio em arquivo, é a data exata que ele traz ou
/// que o usuário informou; quando ainda não veio, é o dia de vencimento do cartão no mês. A ordem é a da data.
/// </summary>
public sealed class VencimentoFatura : SimpleValueObject<DateOnly>
{
    private VencimentoFatura(DateOnly value) : base(value)
    {
    }

    /// <summary>O mês em que a fatura vence, o da data.</summary>
    public MesReferencia Mes => Value;

    /// <summary>O vencimento exato da fatura que veio em arquivo.</summary>
    public static VencimentoFatura Criar(DateOnly data) => new(data);

    /// <summary>O vencimento projetado da fatura que ainda não veio: o dia de vencimento do cartão no mês.</summary>
    public static VencimentoFatura Projetado(MesReferencia mes, DiaVencimentoCartao dia) => new(mes.NoDia(dia));

    public static explicit operator VencimentoFatura(DateOnly data) => Criar(data);
}
