using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Cartoes;

/// <summary>
/// A fatura aberta vem do <c>FaturaCartaoAbertaGenerator</c>, que a tira de um candidato de 100,00. O total zero e o
/// negativo, que não abrem a fatura, estão nos testes do serviço.
/// </summary>
public sealed class FaturaCartaoAbertaTests
{
    [Theory, DomainAutoData]
    public void AceitaPagamento_Quando_TotalNaJanela_Deve_SerVerdadeiro([Frozen] VencimentoFatura vencimento, FaturaCartaoAberta sut)
    {
        DateOnly ultimoDiaJanela = vencimento.Value.AddDays(VencimentoFatura.DiasDepoisVencimento);

        bool result = sut.AceitaPagamento((ReaisPositivo)100m, ultimoDiaJanela);

        result.Should().BeTrue();
    }

    [Theory, DomainAutoData]
    public void AceitaPagamento_Quando_OutroValor_Deve_SerFalso([Frozen] VencimentoFatura vencimento, FaturaCartaoAberta sut)
    {
        bool result = sut.AceitaPagamento((ReaisPositivo)99.99m, vencimento);

        result.Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void AceitaPagamento_Quando_DataForaDaJanela_Deve_SerFalso([Frozen] VencimentoFatura vencimento, FaturaCartaoAberta sut)
    {
        DateOnly diaSeguinteJanela = vencimento.Value.AddDays(VencimentoFatura.DiasDepoisVencimento + 1);

        bool result = sut.AceitaPagamento((ReaisPositivo)100m, diaSeguinteJanela);

        result.Should().BeFalse();
    }
}
