using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Shared.ValueObjects;

public sealed class PeriodoTests
{
    [Theory]
    [InlineDomainAutoData(0)]
    [InlineDomainAutoData(30)]
    public void Criar_Quando_FimNoInicioOuDepois_Deve_RetornarSucesso(int diasAlemDoInicio, DateOnly inicio)
    {
        DateOnly fim = inicio.AddDays(diasAlemDoInicio);

        Result<Periodo> result = Periodo.Criar(inicio, fim);

        Periodo periodo = result.Should().Succeed().Subject;
        periodo.Inicio.Should().Be(inicio);
        periodo.Fim.Should().Be(fim);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_FimAntesDoInicio_Deve_RetornarFalha(DateOnly inicio)
    {
        Result<Periodo> result = Periodo.Criar(inicio, inicio.AddDays(-1));

        result.Should().FailWith("O início do período não pode ser depois do fim.");
    }

    [Theory, DomainAutoData]
    public void Construtor_Quando_FimNoInicioOuDepois_Deve_GuardarAsDatas(DateOnly inicio)
    {
        Periodo result = new(inicio, inicio.AddDays(1));

        result.Inicio.Should().Be(inicio);
        result.Fim.Should().Be(inicio.AddDays(1));
    }

    [Theory, DomainAutoData]
    public void Construtor_Quando_FimAntesDoInicio_Deve_LancarExcecao(DateOnly inicio)
    {
        Func<Periodo> act = () => new Periodo(inicio, inicio.AddDays(-1));

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("fim");
    }

    [Theory, DomainAutoData]
    public void Contem_Quando_DataNasBordas_Deve_RetornarTrue(Periodo sut)
    {
        sut.Contem(sut.Inicio).Should().BeTrue();
        sut.Contem(sut.Fim).Should().BeTrue();
    }

    [Theory, DomainAutoData]
    public void Contem_Quando_DataForaDoPeriodo_Deve_RetornarFalse(Periodo sut)
    {
        sut.Contem(sut.Inicio.AddDays(-1)).Should().BeFalse();
        sut.Contem(sut.Fim.AddDays(1)).Should().BeFalse();
    }

    // As datas são o assunto: 5 dias antes de 10/03 e 3 depois de 20/03.
    [Fact]
    public void Ampliado_Deve_ComecarAntesETerminarDepois()
    {
        Periodo sut = new Periodo(new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 20));

        Periodo result = sut.Ampliado(diasAntes: 5, diasDepois: 3);

        result.Should().Be(new Periodo(new DateOnly(2026, 3, 5), new DateOnly(2026, 3, 23)));
    }

    [Theory, DomainAutoData]
    public void Ampliado_Quando_DiasAntesNegativo_Deve_LancarExcecao(Periodo sut)
    {
        Action act = () => sut.Ampliado(diasAntes: -1, diasDepois: 0);

        act.Should().ThrowExactly<ArgumentOutOfRangeException>().WithParameterName("diasAntes");
    }

    [Theory, DomainAutoData]
    public void Ampliado_Quando_DiasDepoisNegativo_Deve_LancarExcecao(Periodo sut)
    {
        Action act = () => sut.Ampliado(diasAntes: 0, diasDepois: -1);

        act.Should().ThrowExactly<ArgumentOutOfRangeException>().WithParameterName("diasDepois");
    }
}
