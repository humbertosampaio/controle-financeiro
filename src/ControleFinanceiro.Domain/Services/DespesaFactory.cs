using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using CSharpFunctionalExtensions.ValueTasks;

namespace ControleFinanceiro.Domain.Services;

public static class DespesaFactory
{
    public static Result<Despesa> Criar(Usuario usuario, DespesaRecorrente despesaRecorrente, MesReferencia mesReferencia)
    {
        return despesaRecorrente.FormaPagamento == FormaPagamento.Credito
            ? CriarAdiada(usuario, despesaRecorrente, mesReferencia)
            : CriarSimples(usuario, despesaRecorrente, mesReferencia);
    }

    private static Result<Despesa> CriarAdiada(Usuario usuario, DespesaRecorrente despesaRecorrente, MesReferencia mesReferencia)
    {
        Cartao? cartao = usuario.Cartoes.FirstOrDefault(c => c.Id == despesaRecorrente.IdCartao);
        if (cartao is null)
            return Result.Failure<Despesa>("O cartão da despesa recorrente não pertence ao usuário.");

        DateTimeOffset dataCompra = mesReferencia.Anterior()
            .NoDia(despesaRecorrente.DiaCompra)
            .ToLocalDateTimeOffset();

        return DespesaAdiada
            .Criar(
                usuario.Id,
                despesaRecorrente.IdBeneficiario,
                despesaRecorrente.Descricao,
                despesaRecorrente.IdCategoria,
                dataCompra,
                despesaRecorrente.Valor,
                cartao.DiaVencimento);
    }

    private static Result<Despesa> CriarSimples(Usuario usuario, DespesaRecorrente despesaRecorrente, MesReferencia mesReferencia)
    {
        DateTimeOffset dataCompra = mesReferencia.NoDia(despesaRecorrente.DiaCompra).ToLocalDateTimeOffset();
        return DespesaSimples
            .Criar(
                usuario.Id,
                despesaRecorrente.IdBeneficiario,
                despesaRecorrente.Descricao,
                despesaRecorrente.IdCategoria,
                dataCompra,
                despesaRecorrente.FormaPagamento,
                despesaRecorrente.Valor)
            .Map(despesa => despesa as Despesa);
    }
}
