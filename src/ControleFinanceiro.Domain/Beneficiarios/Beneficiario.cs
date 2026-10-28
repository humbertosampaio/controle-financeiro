using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Beneficiarios;

[ValueObject]
public readonly partial struct IdBeneficiario
{
    private static Validation Validate(Guid input) => Id.Validar(input);
}

/// <summary>
/// Quem recebeu o dinheiro de uma saída. É reconhecido no arquivo só pelos aliases, nunca pelo nome, que o usuário
/// escolhe livremente.
/// </summary>
public sealed class Beneficiario : AggregateRoot<IdBeneficiario>, IPertenceAoUsuario
{
    public IdUsuario IdUsuario { get; private set; }
    public TextoObrigatorio Nome { get; private set; } = null!;
    private readonly List<AliasBeneficiario> _aliases = [];
    public IReadOnlyList<AliasBeneficiario> Aliases => _aliases.AsReadOnly();

    private Beneficiario()
    {
    }

    private Beneficiario(IdBeneficiario id, IdUsuario idUsuario, TextoObrigatorio nome)
        : base(id)
    {
        IdUsuario = idUsuario;
        Nome = nome;
    }

    /// <remarks>
    /// Interno pois um alias aponta para um beneficiário só em cada usuário, e só a
    /// <see cref="Services.BeneficiarioFactory"/>, que recebe os beneficiários do usuário, confere isso.
    /// </remarks>
    internal static Beneficiario Criar(IdUsuario idUsuario, TextoObrigatorio nome) =>
        new(IdBeneficiario.FromNewVersion7Guid(), idUsuario, nome);

    /// <remarks>
    /// Interno pelo mesmo motivo do <see cref="Criar"/>. O alias que o beneficiário já tem não muda nada.
    /// </remarks>
    /// <returns>Se o alias foi adicionado, ou seja, se o beneficiário ainda não o tinha.</returns>
    internal bool AdicionarAlias(Alias alias)
    {
        if (PossuiAlias(alias))
            return false;

        _aliases.Add(AliasBeneficiario.Criar(alias, IdUsuario));
        return true;
    }

    /// <summary>O alias que o beneficiário não tem não muda nada.</summary>
    public void RemoverAlias(Alias alias) => _aliases.RemoveAll(a => a.Alias == alias);

    public void Renomear(TextoObrigatorio nome) => Nome = nome;

    public bool PossuiAlias(Alias alias) => _aliases.Exists(a => a.Alias == alias);
}
