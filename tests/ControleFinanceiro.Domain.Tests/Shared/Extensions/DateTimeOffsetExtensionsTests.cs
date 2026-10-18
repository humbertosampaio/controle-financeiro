namespace ControleFinanceiro.Domain.Tests.Shared.Extensions;

public sealed class DateTimeOffsetExtensionsTests
{
    [Fact]
    public void ToDateOnly_Deve_RetornarMesmaDataCalendarioQueLocalDateTime()
    {
        DateTime local = new(2023, 8, 9, 14, 45, 30, DateTimeKind.Local);
        DateTimeOffset sut = new(local);

        DateOnly resultado = sut.ToDateOnly();

        resultado.Should().Be(new DateOnly(2023, 8, 9));
    }

    [Fact]
    public void ToDateOnly_Deve_SerConsistenteComIdaEVoltaAPartirDeDateOnly()
    {
        DateOnly original = new(2021, 1, 5);

        DateOnly volta = original.ToLocalDateTimeOffset().ToDateOnly();

        volta.Should().Be(original);
    }
}
