using AutoFixture.Kernel;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

/// <summary>
/// Monta a <see cref="Fatura"/> pelo <c>Criar</c>, que devolve <c>Result</c>, com os lançamentos do contexto. Com
/// <c>[Frozen] SufixoCartao</c> no teste, todos os lançamentos são do mesmo cartão.
/// </summary>
public sealed class FaturaGenerator : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type t || t != typeof(Fatura))
            return new NoSpecimen();

        return Fatura
            .Criar(context.Create<Banco>(), context.CreateMany<LancamentoFatura>())
            .Value;
    }
}
