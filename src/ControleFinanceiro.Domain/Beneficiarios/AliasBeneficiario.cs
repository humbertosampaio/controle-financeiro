using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Beneficiarios;

/// <summary>
/// Um texto do arquivo que aponta para o beneficiário. Guarda o dono do beneficiário para o banco garantir um só
/// beneficiário por alias em cada usuário.
/// </summary>
public sealed class AliasBeneficiario : ValueObject
{
    public Alias Alias { get; private set; } = null!;
    public IdUsuario IdUsuario { get; private set; }

    private AliasBeneficiario()
    {
    }

    private AliasBeneficiario(Alias alias, IdUsuario idUsuario)
    {
        Alias = alias;
        IdUsuario = idUsuario;
    }

    internal static AliasBeneficiario Criar(Alias alias, IdUsuario idUsuario) => new(alias, idUsuario);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Alias;
        yield return IdUsuario;
    }
}
