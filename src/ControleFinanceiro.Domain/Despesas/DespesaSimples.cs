using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using static ControleFinanceiro.Domain.Despesas.FormaPagamento;

namespace ControleFinanceiro.Domain.Despesas;

public sealed class DespesaSimples : DespesaInteira
{
    private DespesaSimples(
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
            idDespesaRecorrente,
            mesReferencia,
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
            mesReferencia: dataOcorrencia,
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
            mesReferencia: dataOcorrencia,
            pagamento);
    }
}
