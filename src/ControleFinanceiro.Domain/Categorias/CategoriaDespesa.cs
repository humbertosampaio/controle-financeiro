using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Categorias;

[ValueObject]
public readonly partial struct IdCategoriaDespesa
{
    private static Validation Validate(Guid input) => Id.Validar(input);
}

/// <summary>
/// Uma categoria de despesa do usuário. Os aliases são as categorias do banco que o usuário associou a ela; a busca
/// casa só com eles, nunca com o nome. Todo usuário tem a categoria <see cref="NomeOutros"/>.
/// </summary>
public sealed class CategoriaDespesa : AggregateRoot<IdCategoriaDespesa>, IPertenceAoUsuario
{
    /// <summary>
    /// O nome da categoria padrão de cada usuário, onde ele põe as categorias do banco que quer deixar de lado. É
    /// achada pelo nome, e por isso não pode ser renomeada.
    /// </summary>
    public static readonly TextoObrigatorio NomeOutros = (TextoObrigatorio)"Outros";

    public IdUsuario IdUsuario { get; private set; }
    public TextoObrigatorio Nome { get; private set; } = null!;
    private readonly List<AliasCategoria> _aliases = [];
    public IReadOnlyList<AliasCategoria> Aliases => _aliases.AsReadOnly();

    public bool EhOutros => PossuiNome(NomeOutros);

    private CategoriaDespesa()
    {
    }

    private CategoriaDespesa(IdCategoriaDespesa id, IdUsuario idUsuario, TextoObrigatorio nome)
        : base(id)
    {
        IdUsuario = idUsuario;
        Nome = nome;
    }

    /// <remarks>
    /// Interno pois o nome e cada alias são únicos em cada usuário, e só a
    /// <see cref="Services.CategoriaDespesaFactory"/>, que recebe as categorias do usuário, confere isso.
    /// </remarks>
    internal static CategoriaDespesa Criar(IdUsuario idUsuario, TextoObrigatorio nome) =>
        new(IdCategoriaDespesa.FromNewVersion7Guid(), idUsuario, nome);

    /// <remarks>
    /// Interno pelo mesmo motivo do <see cref="Criar"/>. O alias que a categoria já tem não muda nada.
    /// </remarks>
    /// <returns>Se o alias foi adicionado, ou seja, se a categoria ainda não o tinha.</returns>
    internal bool AdicionarAlias(Alias alias)
    {
        if (PossuiAlias(alias))
            return false;

        _aliases.Add(AliasCategoria.Criar(alias, IdUsuario));
        return true;
    }

    /// <summary>O alias que a categoria não tem não muda nada.</summary>
    public void RemoverAlias(Alias alias) => _aliases.RemoveAll(a => a.Alias == alias);

    /// <remarks>Interno pelo mesmo motivo do <see cref="Criar"/>.</remarks>
    internal UnitResult<Erro> Renomear(TextoObrigatorio nome)
    {
        if (EhOutros)
            return ErroCategoriaOutrosNaoRenomeavel.Instancia;

        Nome = nome;
        return UnitResult.Success<Erro>();
    }

    public bool PossuiAlias(Alias alias) => _aliases.Exists(a => a.Alias == alias);

    /// <summary>Compara os nomes sem caixa, acentos e espaços repetidos.</summary>
    public bool PossuiNome(TextoObrigatorio nome) => (TextoNormalizado)Nome == (TextoNormalizado)nome;
}
