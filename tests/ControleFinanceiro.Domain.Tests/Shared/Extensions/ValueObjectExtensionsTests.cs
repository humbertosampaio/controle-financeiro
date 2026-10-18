using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Shared.Extensions;
using ControleFinanceiro.Domain.Usuarios;
using Vogen;

namespace ControleFinanceiro.Domain.Tests.Shared.Extensions;

public sealed class ValueObjectExtensionsTests
{
    [Theory, AutoData]
    public void ToId_Quando_ValidacaoSuceder_DeveConverterTipoPrimitivoParaId(Guid guid)
    {
        IdUsuario result = guid.ToId<Guid, IdUsuario>();

        result.Value.Should().Be(guid);
    }

    [Fact]
    public void ToId_Quando_ValidacaoFalhar_Deve_LancarExcecao()
    {
        Guid guid = Guid.Empty;

        Func<IdUsuario> result = () => guid.ToId<Guid, IdUsuario>();

        result.Should().ThrowExactly<ValueObjectValidationException>();
    }

    [Theory, AutoData]
    public void ToIdGuid_Quando_ValidacaoSuceder_DeveConverterTipoPrimitivoParaId(Guid guid)
    {
        IdUsuario result = guid.ToId<IdUsuario>();

        result.Value.Should().Be(guid);
    }

    [Fact]
    public void ToIdGuid_Quando_ValidacaoFalhar_Deve_LancarExcecao()
    {
        Guid guid = Guid.Empty;

        Func<IdUsuario> result = () => guid.ToId<IdUsuario>();

        result.Should().ThrowExactly<ValueObjectValidationException>();
    }
}
