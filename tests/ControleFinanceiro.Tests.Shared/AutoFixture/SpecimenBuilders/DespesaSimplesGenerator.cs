using AutoFixture.Kernel;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

/// <summary>
/// Monta a <see cref="DespesaSimples"/> pendente, como a geração do mês: no Pix, porque crédito não vale para ela, e sem
/// categoria. Com <c>[Frozen] IdUsuario</c> no teste, a despesa sai desse usuário.
/// </summary>
public sealed class DespesaSimplesGenerator : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type t || t != typeof(DespesaSimples))
            return new NoSpecimen();

        return DespesaSimples
            .Prever(
                context.Create<IdUsuario>(),
                context.Create<IdBeneficiario>(),
                context.Create<TextoObrigatorio>(),
                idCategoria: null,
                context.Create<DateOnly>(),
                FormaPagamento.Pix,
                context.Create<ReaisPositivo>(),
                context.Create<IdDespesaRecorrente>())
            .Value;
    }
}
