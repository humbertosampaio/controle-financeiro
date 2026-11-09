using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Cartoes;

public sealed class VencimentoFaturaTests
{
    [Fact]
    public void OperadorImplicito_Deve_SerODiaDeVencimentoNoMes()
    {
        VencimentoFatura vencimento = new((MesReferencia)(02, 2026), (DiaVencimentoCartao)28);

        DateOnly result = vencimento;

        result.Should().Be(new DateOnly(2026, 02, 28));
    }

    [Theory, DomainAutoData]
    public void Seguinte_Deve_VencerNoMesSeguinteNoMesmoDia(VencimentoFatura vencimento)
    {
        VencimentoFatura result = vencimento.Seguinte();

        result.Mes.Should().Be(vencimento.Mes.Posterior());
        result.Dia.Should().Be(vencimento.Dia);
    }

    [Fact]
    public void Ordem_Deve_SerADaData()
    {
        VencimentoFatura diaMaiorNoMesAnterior = new((MesReferencia)(03, 2026), (DiaVencimentoCartao)28);
        VencimentoFatura diaMenorNoMesSeguinte = new((MesReferencia)(04, 2026), (DiaVencimentoCartao)1);
        VencimentoFatura diaMaiorNoMesSeguinte = new((MesReferencia)(04, 2026), (DiaVencimentoCartao)10);

        VencimentoFatura[] result = new[] { diaMaiorNoMesSeguinte, diaMaiorNoMesAnterior, diaMenorNoMesSeguinte }.Order().ToArray();

        result.Should().Equal(diaMaiorNoMesAnterior, diaMenorNoMesSeguinte, diaMaiorNoMesSeguinte);
    }
}
