using AutoFixture.Kernel;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

/// <summary>
/// Monta a <see cref="DespesaParcelada"/> pela <see cref="DespesaParceladaFactory"/>, a única entrada, a partir de uma
/// compra sem categoria do banco. O usuário é o do cartão: com <c>[Frozen] IdUsuario</c> no teste, a despesa sai desse
/// usuário, e com <c>[Frozen] Cartao</c>, desse cartão.
/// </summary>
public sealed class DespesaParceladaGenerator : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type t || t != typeof(DespesaParcelada))
            return new NoSpecimen();

        Cartao cartao = context.Create<Cartao>();
        TextoObrigatorio descricao = context.Create<TextoObrigatorio>();
        Lancamento compra = Lancamento.Criar(
            context.Create<DateOnly>(),
            hora: null,
            valor: -(Math.Abs(context.Create<int>()) + 1m),
            descricao,
            contraparte: null,
            context.Create<Parcela>(),
            categoriaBanco: null,
            context.Create<IdentidadeLancamento>());

        IReadOnlyList<Beneficiario> beneficiarios = BeneficiarioFactory
            .Resolver(cartao.IdUsuario, beneficiariosDoUsuario: [], [new BeneficiarioNovo(descricao, (Alias)(string)descricao)])
            .Value;

        return DespesaParceladaFactory
            .CriarDaFatura(cartao.IdUsuario, beneficiarios, categoriasDoUsuario: [], cartao, parceladasDoUsuario: [], [compra])
            .Single();
    }
}
