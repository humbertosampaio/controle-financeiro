using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Domain.Despesas;

[ValueObject]
public readonly partial struct IdParcelaDespesa
{
    private static Validation Validate(Guid input) => Id.Validar(input);
}

public sealed class ParcelaDespesa : Entity<IdParcelaDespesa>
{
    public int NumeroParcela { get; private set; }
    public Pagamento Pagamento { get; private set; } = null!;

    /// <summary>A compra da fatura que cobrou a parcela, ou nulo enquanto nenhuma a cobrou.</summary>
    public Cobranca? Cobranca { get; private set; }

    /// <summary>O mês do pagamento. Não é mapeada: numa consulta do EF, use <c>Pagamento.MesReferencia</c>.</summary>
    public MesReferencia MesReferencia => Pagamento.MesReferencia;

    /// <summary>Verdadeiro quando uma compra da fatura cobrou a parcela.</summary>
    public bool TemCobranca => Cobranca is not null;

    public ReaisPositivo ValorProjetado => Pagamento.ValorProjetado;

    /// <summary>O efetivo quando paga, o cobrado quando a fatura a cobrou, senão o projetado.</summary>
    public ReaisPositivo ValorVigente => Pagamento.ValorVigenteCom(Cobranca);

    private ParcelaDespesa()
    {
    }

    private ParcelaDespesa(
        IdParcelaDespesa id,
        int numeroParcela,
        Pagamento pagamento)
            : base(id)
    {
        NumeroParcela = numeroParcela;
        Pagamento = pagamento;
    }

    internal static ParcelaDespesa CriarPendente(int numeroParcela, MesReferencia mesReferencia, DiaVencimentoCartao diaVencimento, ReaisPositivo valor)
    {
        Pagamento pagamento = Pagamento.Pendente(valor, mesReferencia.NoDia(diaVencimento));
        return new ParcelaDespesa(IdParcelaDespesa.FromNewVersion7Guid(), numeroParcela, pagamento);
    }

    /// <summary>A parcela já paga, com o vencimento projetado como data efetiva, a única data que se conhece dela.</summary>
    internal static ParcelaDespesa CriarPaga(int numeroParcela, MesReferencia mesReferencia, DiaVencimentoCartao diaVencimento, ReaisPositivo valor)
    {
        DateOnly data = mesReferencia.NoDia(diaVencimento);
        return new ParcelaDespesa(IdParcelaDespesa.FromNewVersion7Guid(), numeroParcela, Pagamento.Pago(valor, data, data));
    }

    /// <summary>Cobrar não paga: o pagamento continua como está, e a parcela que nasceu paga continua paga.</summary>
    internal UnitResult<Erro> CobrarCom(Cobranca cobranca)
    {
        if (TemCobranca)
            return ErroDespesaJaCobrada.Instancia;

        Cobranca = cobranca;
        return UnitResult.Success<Erro>();
    }

    /// <summary>
    /// Tira a cobrança. A parcela paga por uma transação, a da fatura, falha com <see cref="ErroDespesaJaPaga"/>; a que
    /// nasceu paga, sem transação, perde a cobrança e continua paga.
    /// </summary>
    internal UnitResult<Erro> DesfazerCobranca()
    {
        if (Pagamento.IdTransacao is not null)
            return ErroDespesaJaPaga.Instancia;

        Cobranca = null;
        return UnitResult.Success<Erro>();
    }
}
