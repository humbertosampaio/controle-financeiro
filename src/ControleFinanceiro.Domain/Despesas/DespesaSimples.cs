using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using static ControleFinanceiro.Domain.Despesas.FormaPagamento;

namespace ControleFinanceiro.Domain.Despesas;

public sealed class DespesaSimples : DespesaInteira
{
    private DespesaSimples()
    {
    }

    private DespesaSimples(
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
            idDespesaRecorrente,
            pagamento)
    {
    }

    /// <summary>A despesa já paga, no dia em que ocorreu.</summary>
    public static Result<DespesaSimples> Criar(
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa? idCategoria,
        DateOnly dataOcorrencia,
        FormaPagamento formaPagamento,
        ReaisPositivo valor)
    {
        if (formaPagamento == Credito)
            return Result.Failure<DespesaSimples>($"{Credito} não é uma forma de pagamento válida para despesa simples");

        Pagamento pagamento = Pagamento.Pago(valor, dataOcorrencia, dataOcorrencia);
        return new DespesaSimples(
            IdDespesa.FromNewVersion7Guid(),
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria,
            dataOcorrencia,
            formaPagamento,
            idDespesaRecorrente: null,
            pagamento);
    }

    /// <summary>
    /// A despesa do mês gerada por uma recorrente, ainda pendente, com o pagamento projetado para o dia em que ela
    /// ocorre.
    /// </summary>
    public static Result<DespesaSimples> Prever(
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa? idCategoria,
        DateOnly dataOcorrencia,
        FormaPagamento formaPagamento,
        ReaisPositivo valor,
        IdDespesaRecorrente idDespesaRecorrente)
    {
        if (formaPagamento == Credito)
            return Result.Failure<DespesaSimples>($"{Credito} não é uma forma de pagamento válida para despesa simples");

        Pagamento pagamento = Pagamento.Pendente(valor, dataProjetada: dataOcorrencia);
        return new DespesaSimples(
            IdDespesa.FromNewVersion7Guid(),
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria,
            dataOcorrencia,
            formaPagamento,
            idDespesaRecorrente,
            pagamento);
    }

    /// <summary>
    /// Interno porque só o <see cref="Services.VinculoTransacaoService"/> confere a transação. A despesa que nasceu paga,
    /// sem transação, aceita ser paga pela saída.
    /// </summary>
    internal UnitResult<Erro> QuitarCom(IdTransacao idTransacao, DateOnly dataEfetiva, ReaisPositivo valorEfetivo)
    {
        if (Pagamento.IdTransacao is not null)
            return ErroDespesaJaPaga.Instancia;

        Pagamento = Pagamento.QuitarCom(idTransacao, dataEfetiva, valorEfetivo);
        return UnitResult.Success<Erro>();
    }

    /// <summary>Interno porque só o <see cref="Services.VinculoTransacaoService"/> confere a origem da transação.</summary>
    internal UnitResult<Erro> DesfazerPagamento(IdTransacao idTransacao)
    {
        if (Pagamento.IdTransacao != idTransacao)
            return ErroTransacaoNaoPagouADespesa.Instancia;

        Pagamento = Pagamento.Reabrir();
        return UnitResult.Success<Erro>();
    }

    /// <summary>Sempre nada: a simples não é cobrada, e a saída a paga direto.</summary>
    public override Maybe<Pagamento> PagamentoCobradoPor(IdTransacao idTransacao) => Maybe<Pagamento>.None;
}
