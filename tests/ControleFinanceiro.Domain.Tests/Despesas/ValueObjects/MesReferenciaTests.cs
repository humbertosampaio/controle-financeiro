using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;
using System.ComponentModel.DataAnnotations;

namespace ControleFinanceiro.Domain.Tests.Despesas.ValueObjects;

public sealed class MesReferenciaTests
{
    [Theory, DomainAutoData]
    public void Criar_Quando_MesEAnoValidos_Deve_RetornarSucesso(int ano, [Range(1, 12)] int mes)
    {
        Result<MesReferencia> result = MesReferencia.Criar(mes, ano);

        MesReferencia value = result.Should().Succeed().Which;
        value.Mes.Should().Be(mes);
        value.Ano.Should().Be(ano);
        value.Inicio.Should().Be(new DateOnly(ano, mes, 1));
        value.Fim.Should().Be(new DateOnly(ano, mes, DateTime.DaysInMonth(ano, mes)));
    }

    [Theory]
    [InlineDomainAutoData(0)]
    [InlineDomainAutoData(13)]
    public void Criar_Quando_MesInvalido_Deve_RetornarFalha(int mes, int ano)
    {
        Result<MesReferencia> result = MesReferencia.Criar(mes, ano);

        result.Should().FailWith($"Mês deve estar entre 1 e 12. Valor fornecido: {mes}");
    }

    [Theory]
    [InlineDomainAutoData(-1)]
    [InlineDomainAutoData(-2024)]
    public void Criar_Quando_AnoNegativo_Deve_RetornarFalha(int ano, [Range(1, 12)] int mes)
    {
        Result<MesReferencia> result = MesReferencia.Criar(mes, ano);

        result.Should().FailWith($"Ano não pode ser negativo. Valor fornecido: {ano}");
    }

    [Theory, DomainAutoData]
    public void ConversaoImplicita_De_DateOnly_Deve_UtilizarMesEAno(TimeProvider timeProvider)
    {
        DateTimeOffset agora = timeProvider.GetLocalNow();
        DateOnly dateOnly = DateOnly.FromDateTime(agora.LocalDateTime);

        MesReferencia sut = dateOnly;

        sut.Mes.Should().Be(agora.Month);
        sut.Ano.Should().Be(agora.Year);
    }

    [Theory, DomainAutoData]
    public void Anterior_Quando_Janeiro_Deve_RetornarMesReferenciaEmDezembroDoAnoAnterior([Range(2, 9999)] int ano)
    {
        const int janeiro = 1;
        const int dezembro = 12;
        MesReferencia sut = MesReferencia.Criar(janeiro, ano).Value;

        MesReferencia result = sut.Anterior();

        result.Mes.Should().Be(dezembro);
        result.Ano.Should().Be(ano - 1);
    }

    [Theory, DomainAutoData]
    public void Anterior_Quando_DemaisMeses_Deve_RetornarMesReferenciaNoMesAnteriorDoMesmoAno(int ano, [Range(2, 12)] int mes)
    {
        MesReferencia sut = MesReferencia.Criar(mes, ano).Value;

        MesReferencia result = sut.Anterior();

        result.Mes.Should().Be(mes - 1);
        result.Ano.Should().Be(ano);
    }

    [Theory, DomainAutoData]
    public void Posterior_Quando_Dezembro_Deve_RetornarMesReferenciaEmJaneiroDoAnoPosterior(int ano)
    {
        const int janeiro = 1;
        const int dezembro = 12;
        MesReferencia sut = MesReferencia.Criar(dezembro, ano).Value;

        MesReferencia result = sut.Posterior();

        result.Mes.Should().Be(janeiro);
        result.Ano.Should().Be(ano + 1);
    }

    [Theory, DomainAutoData]
    public void Posterior_Quando_DemaisMeses_Deve_RetornarMesReferenciaNoMesPosteriorDoMesmoAno(int ano, [Range(1, 11)] int mes)
    {
        MesReferencia sut = MesReferencia.Criar(mes, ano).Value;

        MesReferencia result = sut.Posterior();

        result.Mes.Should().Be(mes + 1);
        result.Ano.Should().Be(ano);
    }

    [Theory, DomainAutoData]
    public void NoDia_Quando_DiaValido_Deve_RetornarSucessoComODiaEspecificado(MesReferencia sut, [Range(1, 28)] int dia)
    {
        Result<DateOnly> result = sut.NoDia(dia);

        DateOnly value = result.Should().Succeed().Which;
        value.Year.Should().Be(sut.Ano);
        value.Month.Should().Be(sut.Mes);
        value.Day.Should().Be(dia);
    }

    [Theory, DomainAutoData]
    public void NoDia_Quando_DiaInvalido_Deve_LancarException(MesReferencia sut, [Range(32, int.MaxValue)] int dia)
    {
        Result<DateOnly> result = sut.NoDia(dia);

        result.Should().FailWith($"O dia {dia} é inválido para o mês de referência atual '{sut}'");
    }

    [Theory, DomainAutoData]
    public void Contem_Deve_RetornarTrueSeDataEstiverDentroDoMesReferencia(MesReferencia sut)
    {
        DateOnly dateOnlyDentro = sut.Inicio.AddDays(10);
        DateTimeOffset dateTimeOffsetDentro = dateOnlyDentro.ToLocalDateTimeOffset();

        sut.Contem(dateOnlyDentro).Should().BeTrue();
        sut.Contem(dateTimeOffsetDentro).Should().BeTrue();
    }

    [Theory, DomainAutoData]
    public void Contem_Deve_RetornarFalseSeDataForPosteriorAoMesReferencia(MesReferencia sut)
    {
        DateOnly dateOnlyPosterior = sut.Fim.AddDays(1);
        DateTimeOffset dateTimeOffsetPosterior = dateOnlyPosterior.ToLocalDateTimeOffset();

        sut.Contem(dateOnlyPosterior).Should().BeFalse();
        sut.Contem(dateTimeOffsetPosterior).Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void Contem_Deve_RetornarFalseSeDataForAnteriorAoMesReferencia(MesReferencia sut)
    {
        DateOnly dateOnlyAnterior = sut.Inicio.AddDays(-1);
        DateTimeOffset dateTimeOffsetAnterior = dateOnlyAnterior.ToLocalDateTimeOffset();

        sut.Contem(dateOnlyAnterior).Should().BeFalse();
        sut.Contem(dateTimeOffsetAnterior).Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void ToString_Deve_RetornarFormatoMesBarraAno(int ano)
    {
        const int mes = 5;
        MesReferencia sut = MesReferencia.Criar(mes, ano).Value;

        string texto = sut.ToString();

        texto.Should().Be($"05/{ano}");
    }

    [Theory, DomainAutoData]
    public void ConversaoImplicita_De_DateTimeOffset_Deve_UtilizarMesEAno(TimeProvider timeProvider)
    {
        DateTimeOffset agora = timeProvider.GetLocalNow();

        MesReferencia sut = agora;

        sut.Mes.Should().Be(agora.Month);
        sut.Ano.Should().Be(agora.Year);
    }

    [Theory, DomainAutoData]
    public void OperadoresComparacao_Deve_CompararComBaseNoInicio(DateTimeOffset referencia)
    {
        MesReferencia mesReferenciaMenor = referencia;
        MesReferencia mesReferenciaMaior = referencia.AddMonths(1);

        (mesReferenciaMenor > mesReferenciaMaior).Should().BeFalse();
        (mesReferenciaMenor < mesReferenciaMaior).Should().BeTrue();
    }

    [Theory, DomainAutoData]
    public void Equals_Quando_MesEAnoIguais_Deve_RetornarTrue([Frozen] MesReferencia referencia1, MesReferencia referencia2)
    {
        referencia1.Should().Be(referencia2);
        (referencia1 == referencia2).Should().BeTrue();
        referencia1.Equals(referencia2).Should().BeTrue();
        referencia1.GetHashCode().Should().Be(referencia2.GetHashCode());
    }

    [Theory, DomainAutoData]
    public void Equals_Quando_MesOuAnoDiferentes_Deve_RetornarFalse(MesReferencia referencia1, MesReferencia referencia2)
    {
        referencia1.Should().NotBe(referencia2);
        referencia1.GetHashCode().Should().NotBe(referencia2.GetHashCode());
    }
}
