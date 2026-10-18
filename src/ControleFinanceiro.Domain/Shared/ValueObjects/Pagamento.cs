using System.Diagnostics.CodeAnalysis;

namespace ControleFinanceiro.Domain.Shared.ValueObjects;

public sealed class Pagamento : ValueObject
{
    public ReaisPositivo Valor { get; }
    public StatusPagamento Status { get; }
    public DateTimeOffset? DataProjetada { get; }
    public DateTimeOffset? DataEfetiva { get; }

    [MemberNotNullWhen(true, nameof(DataEfetiva))]
    public bool EstaPago => Status == StatusPagamento.Pago;

    private Pagamento(ReaisPositivo valor, StatusPagamento status, DateTimeOffset? dataProjetada, DateTimeOffset? dataEfetiva)
    {
        Valor = valor;
        Status = status;
        DataProjetada = dataProjetada;
        DataEfetiva = dataEfetiva;
    }

    public static Pagamento Pendente(ReaisPositivo valor, DateOnly? dataProjetada = null) =>
        Pendente(valor, dataProjetada?.ToLocalDateTimeOffset());

    public static Pagamento Pendente(ReaisPositivo valor, DateTimeOffset? dataProjetada = null) =>
        new(valor, StatusPagamento.Pendente, dataProjetada, null);

    public static Pagamento Pago(ReaisPositivo valor, DateTimeOffset dataProjetada, DateTimeOffset dataEfetiva) =>
        new(valor, StatusPagamento.Pago, dataProjetada, dataEfetiva);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
        yield return Status;
        yield return DataProjetada;
        yield return DataEfetiva;
    }
}