using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Importacoes;

public sealed class ImportacaoExtratoTests
{
    [Theory, DomainAutoData]
    public void Registrar_Deve_GuardarOsDadosDaImportacao(
        OrigemTransacao origem,
        FormatoExtrato formato,
        PeriodoExtrato periodo,
        byte novas,
        byte jaExistentes,
        DateTimeOffset importadaEm)
    {
        ImportacaoExtrato result = ImportacaoExtrato.Registrar(origem, formato, periodo, novas + jaExistentes, novas, importadaEm);

        result.Origem.Should().Be(origem);
        result.Formato.Should().Be(formato);
        result.Periodo.Should().Be(periodo);
        result.ImportadaEm.Should().Be(importadaEm);
        result.Lidas.Should().Be(novas + jaExistentes);
        result.Novas.Should().Be(novas);
        result.JaExistentes.Should().Be(jaExistentes);
    }

    [Theory]
    [InlineDomainAutoData(1, -1)]
    [InlineDomainAutoData(1, 2)]
    public void Registrar_Quando_NovasForaDeZeroAteLidas_Deve_LancarExcecao(
        int lidas,
        int novas,
        OrigemTransacao origem,
        FormatoExtrato formato,
        PeriodoExtrato periodo,
        DateTimeOffset importadaEm)
    {
        Action registrar = () => ImportacaoExtrato.Registrar(origem, formato, periodo, lidas, novas, importadaEm);

        registrar.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("novas");
    }

    [Theory, DomainAutoData]
    public void Registrar_Deve_GerarIdVersao7(
        OrigemTransacao origem,
        FormatoExtrato formato,
        PeriodoExtrato periodo,
        byte lidas,
        DateTimeOffset importadaEm)
    {
        ImportacaoExtrato result = ImportacaoExtrato.Registrar(origem, formato, periodo, lidas, lidas, importadaEm);

        result.Id.Value.Version.Should().Be(7);
    }

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
        ImportacaoExtrato ultima,
        Fixture fixture)
    {
        DateOnly inicio = ultima.Periodo.Fim.AddDays(diasDoFimAnteriorAoInicio);
        ImportacaoExtrato sut = Com(fixture, ultima.Origem, PeriodoExtrato.Criar(inicio, inicio.AddDays(30)).Value);

        Maybe<PeriodoExtrato> result = sut.PeriodoNaoImportado(ultima);

        result.Should().HaveNoValue();
    }

    [Theory, DomainAutoData]
    public void PeriodoNaoImportado_Quando_DiasEntreAsImportacoes_Deve_RetornarOsDiasFaltantes(
        ImportacaoExtrato ultima,
        Fixture fixture)
    {
        DateOnly inicio = ultima.Periodo.Fim.AddDays(4);
        ImportacaoExtrato sut = Com(fixture, ultima.Origem, PeriodoExtrato.Criar(inicio, inicio.AddDays(30)).Value);

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

    private static ImportacaoExtrato Com(Fixture fixture, OrigemTransacao origem, PeriodoExtrato periodo)
    {
        fixture.Inject(origem);
        fixture.Inject(periodo);
        return fixture.Create<ImportacaoExtrato>();
    }
}
