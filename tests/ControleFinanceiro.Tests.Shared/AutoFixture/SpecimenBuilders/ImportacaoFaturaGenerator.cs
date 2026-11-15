using AutoFixture.Kernel;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

/// <summary>
/// Monta a <see cref="ImportacaoArquivo"/> como a importação da fatura de um cartão, pela
/// <see cref="ImportacaoArquivoFactory"/>, como a primeira da fatura. Não entra no <see cref="DomainFixture"/>, que
/// monta a importação do extrato de uma conta: o atributo que quer a da fatura o põe na frente. Com <c>[Frozen] Cartao</c>
/// no teste, a importação sai desse cartão, e com <c>[Frozen] VencimentoFatura</c>, com esse vencimento.
/// </summary>
public sealed class ImportacaoFaturaGenerator : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type t || t != typeof(ImportacaoArquivo))
            return new NoSpecimen();

        Cartao cartao = context.Create<Cartao>();

        return ImportacaoArquivoFactory
            .Registrar(
                cartao,
                cartoesUsuario: [cartao],
                importacoesFaturaMes: [],
                context.Create<LayoutArquivoImportacao>(),
                context.Create<Periodo>(),
                context.Create<VencimentoFatura>(),
                context.Create<byte>(),
                context.Create<DateTimeOffset>())
            .Value;
    }
}
