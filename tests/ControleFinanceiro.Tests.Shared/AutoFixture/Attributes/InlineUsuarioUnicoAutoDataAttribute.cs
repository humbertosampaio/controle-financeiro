using AutoFixture.Xunit2;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

public sealed class InlineUsuarioUnicoAutoDataAttribute : InlineAutoDataAttribute
{
    public InlineUsuarioUnicoAutoDataAttribute(params object?[]? values)
        : base(new UsuarioUnicoAutoDataAttribute(), values)
    {
    }
}
