using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using System.Diagnostics.CodeAnalysis;

namespace ControleFinanceiro.Domain.Despesas;

public sealed class Pagamento : ValueObject
{
    public ReaisPositivo ValorProjetado { get; }
    public StatusPagamento Status { get; }
    /// <summary>O mês da data projetada, guardado à parte para o banco filtrar e indexar por ele.</summary>
    public MesReferencia MesReferencia { get; }
    public DateOnly DataProjetada { get; }
    public DateOnly? DataEfetiva { get; }
    public ReaisPositivo? ValorEfetivo { get; }

    /// <summary>
    /// A transação que pagou: a saída na simples, a saída que pagou a fatura na despesa do cartão. Nulo quando o
    /// pagamento está pendente ou nasceu pago, sem transação.
    /// </summary>
    public IdTransacao? IdTransacao { get; }

    [MemberNotNullWhen(true, nameof(DataEfetiva), nameof(ValorEfetivo))]
    public bool EstaPago => Status == StatusPagamento.Pago;

    /// <summary>
    /// O valor que vale para o pagamento: o efetivo quando pago, o cobrado quando a fatura cobrou a despesa, senão o
    /// projetado.
    /// </summary>
    /// <param name="cobranca">A cobrança da despesa do cartão, ou nulo quando não há.</param>
    public ReaisPositivo ValorVigenteCom(Cobranca? cobranca) => EstaPago ? ValorEfetivo : cobranca?.Valor ?? ValorProjetado;

    private Pagamento(
        ReaisPositivo valorProjetado,
        StatusPagamento status,
        MesReferencia mesReferencia,
        DateOnly dataProjetada,
        DateOnly? dataEfetiva,
        ReaisPositivo? valorEfetivo,
        IdTransacao? idTransacao)
    {
        ValorProjetado = valorProjetado;
        Status = status;
        MesReferencia = mesReferencia;
        DataProjetada = dataProjetada;
        DataEfetiva = dataEfetiva;
        ValorEfetivo = valorEfetivo;
        IdTransacao = idTransacao;
    }

    public static Pagamento Pendente(ReaisPositivo valorProjetado, DateOnly dataProjetada) =>
        new(valorProjetado, StatusPagamento.Pendente, mesReferencia: dataProjetada, dataProjetada, dataEfetiva: null, valorEfetivo: null, idTransacao: null);

    /// <summary>O pagamento já pago sem transação conhecida, com o valor projetado como efetivo.</summary>
    public static Pagamento Pago(ReaisPositivo valorProjetado, DateOnly dataProjetada, DateOnly dataEfetiva) =>
        new(valorProjetado, StatusPagamento.Pago, mesReferencia: dataProjetada, dataProjetada, dataEfetiva, valorEfetivo: valorProjetado, idTransacao: null);

    /// <summary>O mesmo pagamento quitado pela transação. O valor, o mês e a data projetados não mudam.</summary>
    public Pagamento QuitarCom(IdTransacao idTransacao, DateOnly dataEfetiva, ReaisPositivo valorEfetivo) =>
        new(ValorProjetado, StatusPagamento.Pago, MesReferencia, DataProjetada, dataEfetiva, valorEfetivo, idTransacao);

    /// <summary>O mesmo pagamento de volta a pendente, sem os dados efetivos.</summary>
    public Pagamento Reabrir() => Pendente(ValorProjetado, DataProjetada);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ValorProjetado;
        yield return Status;
        yield return MesReferencia;
        yield return DataProjetada;
        yield return DataEfetiva;
        yield return ValorEfetivo;
        yield return IdTransacao;
    }
}
