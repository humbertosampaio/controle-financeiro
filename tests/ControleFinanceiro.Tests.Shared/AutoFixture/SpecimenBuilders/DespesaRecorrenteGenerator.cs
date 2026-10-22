using AutoFixture.Kernel;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

/// <summary>
/// Monta a <see cref="DespesaRecorrente"/> pela <see cref="DespesaRecorrenteFactory"/>, que devolve
/// <c>Result</c>. Só pede um <see cref="Cartao"/> ao contexto quando a forma de pagamento é crédito, e então
/// a despesa é do usuário do cartão, como a factory exige. Com <c>[Frozen] IdUsuario</c> no teste, o cartão e
/// a despesa saem desse usuário; com <c>[Frozen] Cartao</c>, a despesa usa esse cartão.
/// </summary>
public sealed class DespesaRecorrenteGenerator : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type t || t != typeof(DespesaRecorrente))
            return new NoSpecimen();

        FormaPagamento formaPagamento = context.Create<FormaPagamento>();
        Cartao? cartao = formaPagamento == FormaPagamento.Credito ? context.Create<Cartao>() : null;

        return DespesaRecorrenteFactory
            .Criar(
                cartao?.IdUsuario ?? context.Create<IdUsuario>(),
                context.Create<IdBeneficiario>(),
                context.Create<TextoObrigatorio>(),
                context.Create<IdCategoriaDespesa>(),
                formaPagamento,
                cartao,
                context.Create<ReaisPositivo>(),
                context.Create<DiaCompraRecorrente>())
            .Value;
    }
}
