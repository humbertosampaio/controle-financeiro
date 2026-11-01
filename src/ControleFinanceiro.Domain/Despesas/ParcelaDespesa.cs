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
    public MesReferencia MesReferencia { get; private set; } = null!;
    public Pagamento Pagamento { get; private set; } = null!;

    /// <summary>Verdadeiro quando uma transação pagou a parcela. A que nasceu paga não tem transação.</summary>
    public bool TemTransacao => Pagamento.IdTransacao is not null;

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

    /// <summary>
    /// A data efetiva é o vencimento projetado, a mesma convenção do <see cref="CriarPaga"/>. A parcela que nasceu paga,
    /// sem transação, aceita o vínculo.
    /// </summary>
    internal UnitResult<Erro> QuitarCom(IdTransacao idTransacao, ReaisPositivo valorEfetivo)
    {
        if (TemTransacao)
            return ErroDespesaJaPaga.Instancia;

        Pagamento = Pagamento.QuitarCom(idTransacao, Pagamento.DataProjetada!.Value, valorEfetivo);
        return UnitResult.Success<Erro>();
    }

    internal void Reabrir() => Pagamento = Pagamento.Reabrir();
}
