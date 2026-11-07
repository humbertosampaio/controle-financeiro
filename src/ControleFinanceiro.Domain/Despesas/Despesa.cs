using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Despesas;

[ValueObject]
public readonly partial struct IdDespesa
{
    private static Validation Validate(Guid input) => Id.Validar(input);
}

public abstract class Despesa : AggregateRoot<IdDespesa>, IPertenceAoUsuario
{
    public IdUsuario IdUsuario { get; protected set; }
    public IdBeneficiario IdBeneficiario { get; protected set; }
    public TextoObrigatorio Descricao { get; protected set; } = null!;
    public IdCategoriaDespesa? IdCategoria { get; protected set; }
    public DateOnly DataOcorrencia { get; protected set; }
    public FormaPagamento FormaPagamento { get; protected set; } = null!;

    /// <summary>A recorrente que gerou a despesa, ou nulo quando ela não veio de uma.</summary>
    public IdDespesaRecorrente? IdDespesaRecorrente { get; protected set; }

    protected Despesa()
    {
    }

    protected Despesa(
        IdDespesa id,
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa? idCategoria,
        DateOnly dataOcorrencia,
        FormaPagamento formaPagamento,
        IdDespesaRecorrente? idDespesaRecorrente)
            : base(id)
    {
        IdUsuario = idUsuario;
        IdBeneficiario = idBeneficiario;
        Descricao = descricao;
        IdCategoria = idCategoria;
        DataOcorrencia = dataOcorrencia;
        FormaPagamento = formaPagamento;
        IdDespesaRecorrente = idDespesaRecorrente;
    }

    /// <summary>Interno porque só o <see cref="Services.AlteracaoCategoriaDespesaService"/> confere de quem é a categoria.</summary>
    internal void AlterarCategoria(IdCategoriaDespesa idCategoria) => IdCategoria = idCategoria;

    /// <summary>O pagamento que cai no mês de referência, ou nada quando a despesa não tem pagamento nele.</summary>
    public abstract Maybe<Pagamento> PagamentoDoMes(MesReferencia mesReferencia);

    /// <summary>
    /// O valor que vale para o pagamento do mês: o efetivo quando pago, o cobrado quando a fatura o cobrou, senão o
    /// projetado. Nada quando a despesa não tem pagamento no mês.
    /// </summary>
    public abstract Maybe<ReaisPositivo> ValorVigenteDoMes(MesReferencia mesReferencia);

    /// <summary>
    /// O pagamento da despesa ou da parcela que a compra do cartão cobrou, ou nada quando ela não cobrou nenhum desta
    /// despesa. A simples não é cobrada: a saída a paga direto.
    /// </summary>
    public abstract Maybe<Pagamento> PagamentoCobradoPor(IdTransacao idTransacao);

    /// <summary>Os pagamentos desta despesa que a transação realizou, vazio quando ela não pagou nenhum.</summary>
    public abstract IReadOnlyList<Pagamento> PagamentosRealizadosPor(IdTransacao idTransacao);
}
