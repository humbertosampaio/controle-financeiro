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
/// Monta a <see cref="DespesaRecorrente"/> pela <see cref="DespesaRecorrenteFactory"/>, como a primeira recorrente
/// do usuário. Só pede um <see cref="Cartao"/> ao contexto quando a forma de pagamento é crédito, e então a despesa é
/// do usuário do cartão, como a factory exige. O beneficiário e a categoria são criados para esse usuário, pelas
/// factories deles. Com <c>[Frozen] IdUsuario</c> no teste, o cartão e a despesa saem desse usuário; com
/// <c>[Frozen] Cartao</c>, a despesa usa esse cartão.
/// </summary>
public sealed class DespesaRecorrenteGenerator : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type t || t != typeof(DespesaRecorrente))
            return new NoSpecimen();

        FormaPagamento formaPagamento = context.Create<FormaPagamento>();
        Cartao? cartao = formaPagamento == FormaPagamento.Credito ? context.Create<Cartao>() : null;
        IdUsuario idUsuario = cartao?.IdUsuario ?? context.Create<IdUsuario>();

        BeneficiarioNovo beneficiarioNovo = new(context.Create<TextoObrigatorio>(), alias: null);
        Beneficiario beneficiario = BeneficiarioFactory
            .Resolver(idUsuario, beneficiariosDoUsuario: [], [beneficiarioNovo])
            .Value[0];
        CategoriaDespesa categoria = CategoriaDespesaFactory.CriarOutros(idUsuario);

        TextoObrigatorio descricao = context.Create<TextoObrigatorio>();
        ReaisPositivo valor = context.Create<ReaisPositivo>();
        DiaOcorrenciaRecorrente diaOcorrencia = context.Create<DiaOcorrenciaRecorrente>();

        if (cartao is null)
        {
            return DespesaRecorrenteFactory
                .CriarSemCartao(idUsuario, recorrentesDoUsuario: [], beneficiario, descricao, categoria, formaPagamento, valor, diaOcorrencia)
                .Value;
        }

        return DespesaRecorrenteFactory
            .CriarNoCredito(idUsuario, recorrentesDoUsuario: [], beneficiario, descricao, categoria, cartao, valor, diaOcorrencia)
            .Value;
    }
}
