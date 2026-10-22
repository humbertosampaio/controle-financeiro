using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Importacoes;

public sealed class ImportacaoExtratoTests
{
    [Theory, DomainAutoData]
    public void PeriodoNaoImportado_Quando_PrimeiraImportacao_Deve_RetornarNada(ImportacaoExtrato sut)
    {
        Maybe<PeriodoExtrato> result = sut.PeriodoNaoImportado(Maybe.None);

        result.Should().HaveNoValue();
    }

    [Theory]
    [InlineDomainAutoData(1)]
    [InlineDomainAutoData(0)]
    [InlineDomainAutoData(-5)]
    public void PeriodoNaoImportado_Quando_PeriodoContiguoOuSobreposto_Deve_RetornarNada(
        int diasDoFimAnteriorAoInicio,
        ContaBancaria conta,
        PeriodoExtrato periodoAnterior,
        Fixture fixture)
    {
        ImportacaoExtrato ultima = Com(fixture, conta, periodoAnterior);
        DateOnly inicio = ultima.Periodo.Fim.AddDays(diasDoFimAnteriorAoInicio);
        ImportacaoExtrato sut = Com(fixture, conta, PeriodoExtrato.Criar(inicio, inicio.AddDays(30)).Value);

        Maybe<PeriodoExtrato> result = sut.PeriodoNaoImportado(ultima);

        result.Should().HaveNoValue();
    }

    [Theory, DomainAutoData]
    public void PeriodoNaoImportado_Quando_DiasEntreAsImportacoes_Deve_RetornarOsDiasFaltantes(
        ContaBancaria conta,
        PeriodoExtrato periodoAnterior,
        Fixture fixture)
    {
        ImportacaoExtrato ultima = Com(fixture, conta, periodoAnterior);
        DateOnly inicio = ultima.Periodo.Fim.AddDays(4);
        ImportacaoExtrato sut = Com(fixture, conta, PeriodoExtrato.Criar(inicio, inicio.AddDays(30)).Value);

        Maybe<PeriodoExtrato> result = sut.PeriodoNaoImportado(ultima);

        result.Should().HaveValue(PeriodoExtrato.Criar(ultima.Periodo.Fim.AddDays(1), ultima.Periodo.Fim.AddDays(3)).Value);
    }

    [Theory, DomainAutoData]
    public void PeriodoNaoImportado_Quando_UltimaDeOutraOrigem_Deve_LancarExcecao(
        ImportacaoExtrato sut,
        ImportacaoExtrato deOutraOrigem)
    {
        Action conferir = () => sut.PeriodoNaoImportado(deOutraOrigem);

        conferir.Should().Throw<ArgumentException>().WithParameterName("ultima");
    }

    private static ImportacaoExtrato Com(Fixture fixture, ContaBancaria conta, PeriodoExtrato periodo)
    {
        fixture.Inject(conta);
        fixture.Inject(periodo);
        return fixture.Create<ImportacaoExtrato>();
    }
}
