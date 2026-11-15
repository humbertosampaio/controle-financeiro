using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Cartoes;

/// <summary>
/// O vencimento de uma fatura: a data em que ela vence. Quando a fatura veio em arquivo, é a data exata que ele traz ou
/// que o usuário informou; quando ainda não veio, é o dia de vencimento do cartão no mês. A ordem é a da data.
/// </summary>
public sealed class VencimentoFatura : SimpleValueObject<DateOnly>
{
    /// <summary>Quantos dias corridos antes do vencimento a saída da conta ainda é provável pagamento da fatura.</summary>
    /// <remarks>Cobre o pagamento antecipado em até uma semana.</remarks>
    public const int DiasAntesVencimento = 7;

    /// <summary>Quantos dias corridos depois do vencimento a saída da conta ainda é provável pagamento da fatura.</summary>
    /// <remarks>
    /// Cobre o vencimento que o banco empurra para depois de um fim de semana ou feriado e o pagamento feito com até uma
    /// semana de atraso.
    /// </remarks>
    public const int DiasDepoisVencimento = 7;

    private VencimentoFatura(DateOnly value) : base(value)
    {
    }

    /// <summary>O mês em que a fatura vence, o da data.</summary>
    public MesReferencia Mes => Value;

    /// <summary>
    /// Se o <paramref name="outro"/> vence no mesmo mês, mas em outra data: as duas não podem ser da mesma fatura, que tem
    /// um vencimento só.
    /// </summary>
    public bool EhOutraDataNoMesmoMes(VencimentoFatura outro) => Mes == outro.Mes && this != outro;

    /// <summary>
    /// Verdadeiro quando um pagamento na data é provável pagamento da fatura: de <see cref="DiasAntesVencimento"/> dias
    /// antes a <see cref="DiasDepoisVencimento"/> dias depois do vencimento, em dias corridos, com as duas pontas.
    /// </summary>
    public bool AceitaPagamentoEm(DateOnly data) => data >= Value.AddDays(-DiasAntesVencimento) && data <= Value.AddDays(DiasDepoisVencimento);

    /// <summary>
    /// Parte das saídas e chega às faturas: os meses de vencimento das faturas que um pagamento com data no
    /// <paramref name="periodoPagamentos"/> pode ter pago, pelo <see cref="AceitaPagamentoEm"/>, em ordem. A saída paga a
    /// fatura que vence até <see cref="DiasDepoisVencimento"/> dias antes dela ou até <see cref="DiasAntesVencimento"/> dias
    /// depois, então os meses são os do período ampliado nessa medida. O caminho contrário é o
    /// <see cref="MesesPagamentoFaturasVencendoEm"/>.
    /// </summary>
    public static IReadOnlyList<MesReferencia> MesesVencimentoFaturasPagaveisEm(Periodo periodoPagamentos) =>
        MesReferencia.Entre(periodoPagamentos.Ampliado(diasAntes: DiasDepoisVencimento, diasDepois: DiasAntesVencimento));

    /// <summary>
    /// Parte da fatura e chega às saídas: os meses em que pode estar a data de um pagamento aceito, pelo
    /// <see cref="AceitaPagamentoEm"/>, por uma fatura que vence em <paramref name="mesVencimento"/>, em ordem. O vencimento
    /// no primeiro dia do mês aceita pagamento até <see cref="DiasAntesVencimento"/> dias antes, e o do último dia, até
    /// <see cref="DiasDepoisVencimento"/> dias depois, então os meses são os do mês ampliado nessa medida. O caminho contrário
    /// é o <see cref="MesesVencimentoFaturasPagaveisEm"/>.
    /// </summary>
    public static IReadOnlyList<MesReferencia> MesesPagamentoFaturasVencendoEm(MesReferencia mesVencimento) =>
        MesReferencia.Entre(PeriodoPagamentoFaturasVencendoEm(mesVencimento));

    /// <summary>
    /// Os meses de vencimento das faturas que disputam as saídas que podem pagar a fatura que vence em
    /// <paramref name="mesVencimento"/>: as faturas que essas saídas também podem ter pago, pelo
    /// <see cref="MesesVencimentoFaturasPagaveisEm"/>, em ordem. Com a janela de uma semana para cada lado, vão do mês
    /// anterior ao seguinte.
    /// </summary>
    public static IReadOnlyList<MesReferencia> MesesVencimentoFaturasConcorrentes(MesReferencia mesVencimento) =>
        MesesVencimentoFaturasPagaveisEm(PeriodoPagamentoFaturasVencendoEm(mesVencimento));

    /// <summary>
    /// Os dias em que pode estar a data de um pagamento aceito, pelo <see cref="AceitaPagamentoEm"/>, por uma fatura que
    /// vence em <paramref name="mesVencimento"/>: do primeiro dia do mês menos <see cref="DiasAntesVencimento"/> ao último
    /// mais <see cref="DiasDepoisVencimento"/>.
    /// </summary>
    private static Periodo PeriodoPagamentoFaturasVencendoEm(MesReferencia mesVencimento) =>
        mesVencimento.Periodo.Ampliado(DiasAntesVencimento, DiasDepoisVencimento);

    /// <summary>O vencimento exato da fatura que veio em arquivo.</summary>
    public static VencimentoFatura Criar(DateOnly data) => new(data);

    /// <summary>O vencimento projetado da fatura que ainda não veio: o dia de vencimento do cartão no mês.</summary>
    public static VencimentoFatura Projetado(MesReferencia mes, DiaVencimentoCartao dia) => new(mes.NoDia(dia));

    public static explicit operator VencimentoFatura(DateOnly data) => Criar(data);
}
