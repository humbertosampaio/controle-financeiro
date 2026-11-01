using AutoFixture.Kernel;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

/// <summary>
/// Monta a <see cref="DespesaAdiada"/> pendente, sem categoria e sem recorrente, no mês de vencimento que o cartão dá à
/// data. O usuário é o do cartão: com <c>[Frozen] IdUsuario</c> no teste, a despesa sai desse usuário, e com
/// <c>[Frozen] Cartao</c>, desse cartão.
/// </summary>
public sealed class DespesaAdiadaGenerator : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type t || t != typeof(DespesaAdiada))
            return new NoSpecimen();

        Cartao cartao = context.Create<Cartao>();
        DateOnly data = context.Create<DateOnly>();

        return DespesaAdiada.Criar(
            cartao.IdUsuario,
            context.Create<IdBeneficiario>(),
            context.Create<TextoObrigatorio>(),
            idCategoria: null,
            data,
            context.Create<ReaisPositivo>(),
            cartao.Id,
            cartao.MesVencimento(data),
            cartao.DiaVencimento,
            idDespesaRecorrente: null);
    }
}
