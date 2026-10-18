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

    private static DespesaAdiada CriarAdiada(Usuario usuario, DespesaRecorrente despesaRecorrente, MesReferencia mesReferencia)
    {
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
                usuario.Cartao.DiaVencimento);
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
