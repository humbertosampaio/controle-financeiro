using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Importacoes;

public sealed class ImportacaoArquivoTests
{
    [Theory, DomainAutoData]
    public void PeriodoNaoImportado_Quando_PrimeiraImportacao_Deve_RetornarNada(ImportacaoArquivo sut)
    {
        Maybe<PeriodoArquivo> result = sut.PeriodoNaoImportado(Maybe.None);

        result.Should().HaveNoValue();
    }

    [Theory]
    [InlineDomainAutoData(1)]
    [InlineDomainAutoData(0)]
    [InlineDomainAutoData(-5)]
    public void PeriodoNaoImportado_Quando_PeriodoContiguoOuSobreposto_Deve_RetornarNada(
        int diasDoFimAnteriorAoInicio,
        ContaBancaria conta,
        PeriodoArquivo periodoAnterior,
        Fixture fixture)
    {
        ImportacaoArquivo ultima = Com(fixture, conta, periodoAnterior);
        DateOnly inicio = ultima.Periodo.Fim.AddDays(diasDoFimAnteriorAoInicio);
        ImportacaoArquivo sut = Com(fixture, conta, PeriodoArquivo.Criar(inicio, inicio.AddDays(30)).Value);

        Maybe<PeriodoArquivo> result = sut.PeriodoNaoImportado(ultima);

        result.Should().HaveNoValue();
    }

    [Theory, DomainAutoData]
    public void PeriodoNaoImportado_Quando_DiasEntreAsImportacoes_Deve_RetornarOsDiasFaltantes(
        ContaBancaria conta,
        PeriodoArquivo periodoAnterior,
        Fixture fixture)
    {
        ImportacaoArquivo ultima = Com(fixture, conta, periodoAnterior);
        DateOnly inicio = ultima.Periodo.Fim.AddDays(4);
        ImportacaoArquivo sut = Com(fixture, conta, PeriodoArquivo.Criar(inicio, inicio.AddDays(30)).Value);

        Maybe<PeriodoArquivo> result = sut.PeriodoNaoImportado(ultima);

        result.Should().HaveValue(PeriodoArquivo.Criar(ultima.Periodo.Fim.AddDays(1), ultima.Periodo.Fim.AddDays(3)).Value);
    }

    [Theory, DomainAutoData]
    public void PeriodoNaoImportado_Quando_UltimaDeOutraOrigem_Deve_LancarExcecao(
        ImportacaoArquivo sut,
        ImportacaoArquivo deOutraOrigem)
    {
        Action conferir = () => sut.PeriodoNaoImportado(deOutraOrigem);

        conferir.Should().ThrowExactly<ArgumentException>().WithParameterName("ultima");
    }

    private static ImportacaoArquivo Com(Fixture fixture, ContaBancaria conta, PeriodoArquivo periodo)
    {
        fixture.Inject(conta);
        fixture.Inject(periodo);
        return fixture.Create<ImportacaoArquivo>();
    }
}
