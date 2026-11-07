using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Despesas;

public abstract class DespesaInteira : Despesa
{
    public Pagamento Pagamento { get; protected set; } = null!;

    /// <summary>O mês do pagamento. Não é mapeada: numa consulta do EF, use <c>Pagamento.MesReferencia</c>.</summary>
    public MesReferencia MesReferencia => Pagamento.MesReferencia;

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
        Pagamento = pagamento;
    }

    /// <summary>A despesa que nasceu paga, sem transação, aceita o vínculo.</summary>
    private protected UnitResult<Erro> Quitar(IdTransacao idTransacao, DateOnly dataEfetiva, ReaisPositivo valorEfetivo)
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

    public override Maybe<Pagamento> PagamentoDoMes(MesReferencia mesReferencia) =>
        MesReferencia == mesReferencia ? Pagamento : Maybe<Pagamento>.None;

    public override Maybe<MesReferencia> MesQuitado(IdTransacao idTransacao) =>
        Pagamento.IdTransacao == idTransacao ? MesReferencia : Maybe<MesReferencia>.None;
}
