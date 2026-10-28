using ControleFinanceiro.Domain.Shared.ValueObjects;
using System.Diagnostics.CodeAnalysis;

namespace ControleFinanceiro.Domain.Despesas;

public sealed class Pagamento : ValueObject
{
    public ReaisPositivo Valor { get; }
    public StatusPagamento Status { get; }
    public DateOnly? DataProjetada { get; }
    public DateOnly? DataEfetiva { get; }

    [MemberNotNullWhen(true, nameof(DataEfetiva))]
    public bool EstaPago => Status == StatusPagamento.Pago;

    private Pagamento(ReaisPositivo valor, StatusPagamento status, DateOnly? dataProjetada, DateOnly? dataEfetiva)
    {
        Valor = valor;
        Status = status;
        DataProjetada = dataProjetada;
        DataEfetiva = dataEfetiva;
    }

    public static Pagamento Pendente(ReaisPositivo valor, DateOnly? dataProjetada = null) =>
        new(valor, StatusPagamento.Pendente, dataProjetada, dataEfetiva: null);

    public static Pagamento Pago(ReaisPositivo valor, DateOnly dataProjetada, DateOnly dataEfetiva) =>
        new(valor, StatusPagamento.Pago, dataProjetada, dataEfetiva);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
        yield return Status;
        yield return DataProjetada;
        yield return DataEfetiva;
    }
}