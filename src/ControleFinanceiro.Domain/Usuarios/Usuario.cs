using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Usuarios;

[ValueObject]
public readonly partial struct IdUsuario
{
    private static Validation Validate(Guid input) => Id.Validar(input);
}

public sealed class Usuario : AggregateRoot<IdUsuario>
{
    public TextoObrigatorio Nome { get; private set; } = null!;
    public ReaisPositivo RendaBruta { get; private set; } = null!;

    private readonly List<DescontoSalarial> _descontosSalariais = [];
    public IReadOnlyList<DescontoSalarial> DescontosSalariais => _descontosSalariais;

    public Reais RendaLiquida => RendaBruta - _descontosSalariais.Sum(desconto => desconto.Valor);

    private Usuario()
    {
    }

    private Usuario(
        IdUsuario id,
        TextoObrigatorio nome,
        ReaisPositivo rendaBruta,
        IEnumerable<DescontoSalarial> descontosSalariais)
            : base(id)
    {
        Nome = nome;
        RendaBruta = rendaBruta;
        _descontosSalariais = descontosSalariais.ToList();
    }

    public static Usuario Criar(
        TextoObrigatorio nome,
        ReaisPositivo rendaBruta,
        IEnumerable<DescontoSalarial> descontosSalariais)
    {
        return new(IdUsuario.FromNewVersion7Guid(), nome, rendaBruta, descontosSalariais);
    }
}
