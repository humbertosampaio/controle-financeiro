using AutoFixture.Kernel;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

/// <summary>
/// Monta a <see cref="DespesaRecorrente"/> pelo <c>Criar</c>, que devolve <c>Result</c>. Só passa cartão
/// quando a forma de pagamento é crédito; injete o <see cref="IdCartao"/> de um cartão do usuário para
/// que ele aceite a despesa.
/// </summary>
public sealed class DespesaRecorrenteGenerator : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type t || t != typeof(DespesaRecorrente))
            return new NoSpecimen();

        FormaPagamento formaPagamento = context.Create<FormaPagamento>();
        IdCartao? idCartao = formaPagamento == FormaPagamento.Credito ? context.Create<IdCartao>() : null;

        return DespesaRecorrente
            .Criar(
                context.Create<IdBeneficiario>(),
                context.Create<TextoObrigatorio>(),
                context.Create<IdCategoriaDespesa>(),
                formaPagamento,
                idCartao,
                context.Create<ReaisPositivo>(),
                context.Create<DiaCompraRecorrente>())
            .Value;
    }
}
