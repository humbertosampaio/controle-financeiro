namespace ControleFinanceiro.Domain.Tests.Shared.Extensions;

public sealed class DateOnlyExtensionsTests
{
    [Fact]
    public void ToLocalDateTimeOffset_Deve_PreservarDataEComecarAMeiaNoiteLocal()
    {
        DateOnly date = new(2024, 6, 15);

        DateTimeOffset resultado = date.ToLocalDateTimeOffset();

        resultado.Year.Should().Be(date.Year);
        resultado.Month.Should().Be(date.Month);
        resultado.Day.Should().Be(date.Day);
        resultado.Hour.Should().Be(0);
        resultado.Minute.Should().Be(0);
        resultado.Second.Should().Be(0);
        resultado.Millisecond.Should().Be(0);
    }

    [Fact]
    public void ToLocalDateTimeOffset_Deve_SerConsistenteComIdaEVoltaParaDateOnly()
    {
        DateOnly original = new(2022, 11, 30);

        DateOnly volta = original.ToLocalDateTimeOffset().ToDateOnly();

        volta.Should().Be(original);
    }
}
