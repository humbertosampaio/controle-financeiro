using Ardalis.SmartEnum;
using ControleFinanceiro.Domain.Shared.Extensions;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Shared.Extensions;

public sealed class EnumExtensionsTests
{
    [Fact]
    public void TryFromValue_Quando_ValorExiste_Deve_RetornarSucesso()
    {
        TestEnum item1 = TestEnum.Item1;

        Result<TestEnum> resultado = TestEnum.TryFromValue(item1.Value);

        resultado.Should().Succeed()
            .Which.Value.Should().Be(item1.Value);
    }

    [Fact]
    public void TryFromValue_Quando_ValorInexistente_Deve_RetornarFalhaComMensagem()
    {
        const int valorInvalido = 99;

        Result<TestEnum> resultado = TestEnum.TryFromValue(valorInvalido);

        resultado.Should().FailWith($"Valor '{valorInvalido}' não corresponde a um {nameof(TestEnum)} válido");
    }

    private sealed class TestEnum : SmartEnum<TestEnum, int>
    {
        public static readonly TestEnum Item1 = new("Item1", 1);

        private TestEnum(string name, int value) : base(name, value)
        {
        }
    }
}
