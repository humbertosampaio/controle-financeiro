using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Beneficiarios;

[ValueObject]
public readonly partial struct IdBeneficiario
{
    private static Validation Validate(Guid input) => Id.Validar(input);
}

public sealed class Beneficiario : AggregateRoot<IdBeneficiario>
{
    public IdUsuario IdUsuario { get; private set; }
    public TextoObrigatorio NomeExibicao { get; private set; }
    private readonly List<AliasBeneficiario> _aliases = [];
    public IReadOnlyList<AliasBeneficiario> Aliases => _aliases.AsReadOnly();

    private Beneficiario(IdBeneficiario id, IdUsuario idUsuario, TextoObrigatorio nomeExibicao)
        : base(id)
    {
        IdUsuario = idUsuario;
        NomeExibicao = nomeExibicao;
    }

    public static Beneficiario Criar(IdUsuario idUsuario, TextoObrigatorio nomeExibicao) =>
        new(IdBeneficiario.FromNewVersion7Guid(), idUsuario, nomeExibicao);

    public Result AdicionarAlias(TextoObrigatorio textoExtrato, string? codigoOrigem = null)
    {
        AliasBeneficiario novo = AliasBeneficiario.Criar(textoExtrato, codigoOrigem);
        if (_aliases.Contains(novo))
            return Result.Failure("Já existe um alias com o mesmo texto e origem para este beneficiário.");

        _aliases.Add(novo);
        return Result.Success();
    }
}
