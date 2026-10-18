using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using static ControleFinanceiro.Domain.Shared.ValueObjects.FormaPagamento;

namespace ControleFinanceiro.Domain.Despesas;

public sealed class DespesaSimples : DespesaInteira
{
    private DespesaSimples(
        IdDespesa id,
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        DateTimeOffset dataCompra,
        FormaPagamento formaPagamento,
        MesReferencia mesReferencia,
        Pagamento pagamento)
        : base(
            id,
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria,
            dataCompra,
            formaPagamento,
            mesReferencia,
            pagamento)
    {
    }

    public static Result<DespesaSimples> Criar(
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        DateTimeOffset dataCompra,
        FormaPagamento formaPagamento,
        ReaisPositivo valor)
    {
        if (formaPagamento == Credito)
            return Result.Failure<DespesaSimples>($"{Credito} não é uma forma de pagamento válida para despesa simples");

        Pagamento pagamento = Pagamento.Pago(valor, dataCompra, dataCompra);
        return new DespesaSimples(
            IdDespesa.FromNewVersion7Guid(),
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria,
            dataCompra,
            formaPagamento,
            mesReferencia: dataCompra,
            pagamento);
    }
}
