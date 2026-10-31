using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using System.Diagnostics.CodeAnalysis;

namespace ControleFinanceiro.Domain.Despesas;

public sealed class Pagamento : ValueObject
{
    public ReaisPositivo ValorProjetado { get; }
    public StatusPagamento Status { get; }
    public DateOnly? DataProjetada { get; }
    public DateOnly? DataEfetiva { get; }
    public ReaisPositivo? ValorEfetivo { get; }

    /// <summary>A transação que quitou o pagamento, ou nulo quando ele está pendente ou nasceu pago, sem transação.</summary>
    public IdTransacao? IdTransacao { get; }

    [MemberNotNullWhen(true, nameof(DataEfetiva), nameof(ValorEfetivo))]
    public bool EstaPago => Status == StatusPagamento.Pago;

    private Pagamento(
        ReaisPositivo valorProjetado,
        StatusPagamento status,
        DateOnly? dataProjetada,
        DateOnly? dataEfetiva,
        ReaisPositivo? valorEfetivo,
        IdTransacao? idTransacao)
    {
        ValorProjetado = valorProjetado;
        Status = status;
        DataProjetada = dataProjetada;
        DataEfetiva = dataEfetiva;
        ValorEfetivo = valorEfetivo;
        IdTransacao = idTransacao;
    }

    public static Pagamento Pendente(ReaisPositivo valorProjetado, DateOnly? dataProjetada = null) =>
        new(valorProjetado, StatusPagamento.Pendente, dataProjetada, dataEfetiva: null, valorEfetivo: null, idTransacao: null);

    /// <summary>O pagamento já pago sem transação conhecida, com o valor projetado como efetivo.</summary>
    public static Pagamento Pago(ReaisPositivo valorProjetado, DateOnly dataProjetada, DateOnly dataEfetiva) =>
        new(valorProjetado, StatusPagamento.Pago, dataProjetada, dataEfetiva, valorEfetivo: valorProjetado, idTransacao: null);

    /// <summary>O mesmo pagamento quitado pela transação. O valor e a data projetados não mudam.</summary>
    public Pagamento QuitarCom(IdTransacao idTransacao, DateOnly dataEfetiva, ReaisPositivo valorEfetivo) =>
        new(ValorProjetado, StatusPagamento.Pago, DataProjetada, dataEfetiva, valorEfetivo, idTransacao);

    /// <summary>O mesmo pagamento de volta a pendente, sem os dados efetivos.</summary>
    public Pagamento Reabrir() => Pendente(ValorProjetado, DataProjetada);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ValorProjetado;
        yield return Status;
        yield return DataProjetada;
        yield return DataEfetiva;
        yield return ValorEfetivo;
        yield return IdTransacao;
    }
}
