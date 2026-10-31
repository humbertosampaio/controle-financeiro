using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Despesas;

[ValueObject]
public readonly partial struct IdParcelaDespesa
{
    private static Validation Validate(Guid input) => Id.Validar(input);
}

public sealed class ParcelaDespesa : Entity<IdParcelaDespesa>
{
    public int NumeroParcela { get; private set; }
    public MesReferencia MesReferencia { get; private set; } = null!;
    public Pagamento Pagamento { get; private set; } = null!;

    private ParcelaDespesa()
    {
    }

    private ParcelaDespesa(
        IdParcelaDespesa id,
        int numeroParcela,
        MesReferencia mesReferencia,
        Pagamento pagamento)
            : base(id)
    {
        NumeroParcela = numeroParcela;
        MesReferencia = mesReferencia;
        Pagamento = pagamento;
    }

    internal static ParcelaDespesa CriarPendente(int numeroParcela, MesReferencia mesReferencia, DiaVencimentoCartao diaVencimento, ReaisPositivo valor)
    {
        Pagamento pagamento = Pagamento.Pendente(valor, mesReferencia.NoDia(diaVencimento));
        return new ParcelaDespesa(IdParcelaDespesa.FromNewVersion7Guid(), numeroParcela, mesReferencia, pagamento);
    }

    /// <summary>A parcela já paga, com o vencimento projetado como data efetiva, a única data que se conhece dela.</summary>
    internal static ParcelaDespesa CriarPaga(int numeroParcela, MesReferencia mesReferencia, DiaVencimentoCartao diaVencimento, ReaisPositivo valor)
    {
        DateOnly data = mesReferencia.NoDia(diaVencimento);
        return new ParcelaDespesa(IdParcelaDespesa.FromNewVersion7Guid(), numeroParcela, mesReferencia, Pagamento.Pago(valor, data, data));
    }
}
