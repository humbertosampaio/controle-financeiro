using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Despesas;

public abstract class DespesaInteira : Despesa
{
    public Pagamento Pagamento { get; protected set; } = null!;

    /// <summary>O mês do pagamento. Não é mapeada: numa consulta do EF, use <c>Pagamento.MesReferencia</c>.</summary>
    public MesReferencia MesReferencia => Pagamento.MesReferencia;

    /// <summary>O valor que vale para o pagamento: o efetivo quando pago, o projetado quando pendente.</summary>
    public virtual ReaisPositivo ValorVigente => Pagamento.ValorVigenteCom(cobranca: null);

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

    public override Maybe<Pagamento> PagamentoDoMes(MesReferencia mesReferencia) =>
        MesReferencia == mesReferencia ? Pagamento : Maybe<Pagamento>.None;

    public override Maybe<ReaisPositivo> ValorVigenteDoMes(MesReferencia mesReferencia) =>
        MesReferencia == mesReferencia ? ValorVigente : Maybe<ReaisPositivo>.None;

    public override IReadOnlyList<Pagamento> PagamentosRealizadosPor(IdTransacao idTransacao) =>
        Pagamento.IdTransacao == idTransacao ? [Pagamento] : [];
}
