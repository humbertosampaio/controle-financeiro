using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Despesas;

public abstract class DespesaInteira : Despesa
{
    public MesReferencia MesReferencia { get; protected set; } = null!;
    public Pagamento Pagamento { get; protected set; } = null!;

    protected DespesaInteira()
    {
    }

    protected DespesaInteira(
        IdDespesa id,
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa? idCategoria,
        DateOnly dataOcorrencia,
        FormaPagamento formaPagamento,
        IdDespesaRecorrente? idDespesaRecorrente,
        MesReferencia mesReferencia,
        Pagamento pagamento)
        : base(
            id,
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria,
            dataOcorrencia,
            formaPagamento,
            idDespesaRecorrente)
    {
        MesReferencia = mesReferencia;
        Pagamento = pagamento;
    }

    /// <summary>
    /// Interno porque só o <see cref="Services.VinculoPagamentoService"/> confere a transação. A despesa que nasceu paga,
    /// sem transação, aceita o vínculo.
    /// </summary>
    internal UnitResult<Erro> QuitarCom(IdTransacao idTransacao, DateOnly dataEfetiva, ReaisPositivo valorEfetivo)
    {
        if (Pagamento.IdTransacao is not null)
            return ErroDespesaJaPaga.Instancia;

        Pagamento = Pagamento.QuitarCom(idTransacao, dataEfetiva, valorEfetivo);
        return UnitResult.Success<Erro>();
    }

    public override UnitResult<Erro> DesfazerVinculo(IdTransacao idTransacao)
    {
        if (Pagamento.IdTransacao != idTransacao)
            return ErroTransacaoNaoVinculada.Instancia;

        Pagamento = Pagamento.Reabrir();
        return UnitResult.Success<Erro>();
    }
}
