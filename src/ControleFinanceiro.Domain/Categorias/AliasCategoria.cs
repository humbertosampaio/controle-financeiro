using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Categorias;

/// <summary>
/// Uma categoria do banco que aponta para a categoria do usuário. Guarda o dono da categoria para o banco garantir
/// uma só categoria por alias em cada usuário.
/// </summary>
public sealed class AliasCategoria : ValueObject
{
    public Alias Alias { get; private set; } = null!;
    public IdUsuario IdUsuario { get; private set; }

    private AliasCategoria()
    {
    }

    private AliasCategoria(Alias alias, IdUsuario idUsuario)
    {
        Alias = alias;
        IdUsuario = idUsuario;
    }

    internal static AliasCategoria Criar(Alias alias, IdUsuario idUsuario) => new(alias, idUsuario);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Alias;
        yield return IdUsuario;
    }
}
