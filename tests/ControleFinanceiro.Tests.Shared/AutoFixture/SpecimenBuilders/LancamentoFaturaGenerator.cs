using AutoFixture.Kernel;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

/// <summary>
/// Monta o <see cref="LancamentoFatura"/> com o lançamento do contexto, como uma linha de fatura. Com
/// <c>[Frozen] SufixoCartao</c> no teste, o lançamento é desse cartão.
/// </summary>
public sealed class LancamentoFaturaGenerator : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type t || t != typeof(LancamentoFatura))
            return new NoSpecimen();

        return LancamentoFatura.Criar(context.Create<Lancamento>().NaOrigem(TipoOrigem.Cartao), context.Create<SufixoCartao>());
    }
}
