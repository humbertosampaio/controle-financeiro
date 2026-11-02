using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

/// <summary>Tudo que o fixture montar sai do mesmo usuário, como a conta e a transação do <see cref="TransacoesDeTeste"/>.</summary>
public sealed class UsuarioUnicoAutoDataAttribute : DomainAutoDataAttribute
{
    public UsuarioUnicoAutoDataAttribute()
        : base(fixture => fixture.Freeze<IdUsuario>())
    {
    }
}
