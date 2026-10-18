using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using Vogen;

namespace ControleFinanceiro.Domain.Tests.Shared.ValueObjects;

public sealed class IdTests
{
    [Theory, AutoData]
    public void Validate_Quando_ValorValido_Deve_RetornarSucesso(Guid valor)
    {
        Validation result = Id.Validar(valor);

        result.Should().Be(Validation.Ok);
    }

    [Fact]
    public void Validate_Quando_ValorInvalido_Deve_RetornarFalha()
    {
        Validation result = Id.Validar(Guid.Empty);

        result.Should().BeEquivalentTo(Validation.Invalid("Id não pode ser vazio"));
    }
}
