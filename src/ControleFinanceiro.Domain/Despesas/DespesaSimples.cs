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
            mesReferencia,
            pagamento)
    {
    }

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
            mesReferencia: dataOcorrencia,
            pagamento);
    }
}
