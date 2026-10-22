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
    public MesReferencia MesReferencia { get; private set; }
    public Pagamento Pagamento { get; private set; }

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

    public static Result<ParcelaDespesa> Criar(int numeroParcela, MesReferencia mesReferencia, DiaVencimentoCartao diaVencimento, ReaisPositivo valor)
    {
        if (numeroParcela <= 0)
            return Result.Failure<ParcelaDespesa>("Número da parcela deve ser maior que zero.");

        Pagamento pagamento = Pagamento.Pendente(valor, mesReferencia.NoDia(diaVencimento));
        return new ParcelaDespesa(IdParcelaDespesa.FromNewVersion7Guid(), numeroParcela, mesReferencia, pagamento);
    }
}
