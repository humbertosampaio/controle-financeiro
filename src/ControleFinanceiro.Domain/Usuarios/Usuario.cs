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

    public Reais RendaLiquida => RendaBruta - DescontosSalariais.Sum(desconto => desconto.Valor);

    private readonly List<DespesaRecorrente> _despesasRecorrentes = [];
    public IReadOnlyList<DespesaRecorrente> DespesasRecorrentes => _despesasRecorrentes;
    public Cartao Cartao { get; private set; } = null!;

    private Usuario()
    {
    }

    private Usuario(
        IdUsuario id,
        TextoObrigatorio nome,
        ReaisPositivo rendaBruta,
        IEnumerable<DescontoSalarial> descontosSalariais,
        Cartao cartao)
            : base(id)
    {
        Nome = nome;
        RendaBruta = rendaBruta;
        _descontosSalariais = descontosSalariais.ToList();
        Cartao = cartao;
    }

    public static Usuario Criar(
        TextoObrigatorio nome,
        ReaisPositivo rendaBruta,
        IEnumerable<DescontoSalarial> descontosSalariais,
        Cartao cartao)
    {
        return new(IdUsuario.FromNewVersion7Guid(), nome, rendaBruta, descontosSalariais, cartao);
    }

    public Result AdicionarDespesaRecorrente(DespesaRecorrente despesaRecorrente)
    {
        if (DespesasRecorrentes.Contains(despesaRecorrente))
            return Result.Failure("Despesa recorrente já existe para este usuário.");

        _despesasRecorrentes.Add(despesaRecorrente);
        return Result.Success();
    }
}
