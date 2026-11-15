using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Cartoes;

public sealed class VencimentoFaturaTests
{
    [Fact]
    public void Projetado_Deve_SerODiaDeVencimentoNoMes()
    {
        VencimentoFatura result = VencimentoFatura.Projetado((MesReferencia)(02, 2026), (DiaVencimentoCartao)24);

        result.Value.Should().Be(new DateOnly(2026, 02, 24));
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_GuardarADataExata(DateOnly data)
    {
        VencimentoFatura result = VencimentoFatura.Criar(data);

        result.Value.Should().Be(data);
    }

    [Theory, DomainAutoData]
    public void OperadorExplicito_Deve_SerODaDataExata(DateOnly data)
    {
        VencimentoFatura result = (VencimentoFatura)data;

        result.Should().Be(VencimentoFatura.Criar(data));
    }

    [Theory, DomainAutoData]
    public void Mes_Deve_SerOMesDaData(DateOnly data)
    {
        VencimentoFatura.Criar(data).Mes.Should().Be((MesReferencia)data);
    }

    [Theory, DomainAutoData]
    public void Equals_Quando_DataExataIgualAoProjetado_Deve_SerIgual(MesReferencia mes, DiaVencimentoCartao dia)
    {
        VencimentoFatura projetado = VencimentoFatura.Projetado(mes, dia);

        VencimentoFatura.Criar(mes.NoDia(dia)).Should().Be(projetado);
    }

    [Theory, DomainAutoData]
    public void EhOutraDataNoMesmoMes_Quando_MesmaData_Deve_SerFalso(DateOnly data)
    {
        VencimentoFatura sut = VencimentoFatura.Criar(data);

        sut.EhOutraDataNoMesmoMes(VencimentoFatura.Criar(data)).Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void EhOutraDataNoMesmoMes_Quando_OutraDataNoMesmoMes_Deve_SerVerdadeiro(
        MesReferencia mes,
        DiaVencimentoCartao dia,
        DiaVencimentoCartao outroDia)
    {
        VencimentoFatura sut = VencimentoFatura.Projetado(mes, dia);

        sut.EhOutraDataNoMesmoMes(VencimentoFatura.Projetado(mes, outroDia)).Should().BeTrue();
    }

    [Theory, DomainAutoData]
    public void EhOutraDataNoMesmoMes_Quando_OutroMes_Deve_SerFalso(MesReferencia mes, MesReferencia outroMes, DiaVencimentoCartao dia)
    {
        VencimentoFatura sut = VencimentoFatura.Projetado(mes, dia);

        sut.EhOutraDataNoMesmoMes(VencimentoFatura.Projetado(outroMes, dia)).Should().BeFalse();
    }

    [Fact]
    public void Ordem_Deve_SerADaData()
    {
        VencimentoFatura diaMaiorNoMesAnterior = VencimentoFatura.Projetado((MesReferencia)(03, 2026), (DiaVencimentoCartao)24);
        VencimentoFatura diaMenorNoMesSeguinte = VencimentoFatura.Projetado((MesReferencia)(04, 2026), (DiaVencimentoCartao)1);
        VencimentoFatura diaMaiorNoMesSeguinte = VencimentoFatura.Criar(new DateOnly(2026, 04, 13));

        VencimentoFatura[] result = new[] { diaMaiorNoMesSeguinte, diaMaiorNoMesAnterior, diaMenorNoMesSeguinte }.Order().ToArray();

        result.Should().Equal(diaMaiorNoMesAnterior, diaMenorNoMesSeguinte, diaMaiorNoMesSeguinte);
    }
}
