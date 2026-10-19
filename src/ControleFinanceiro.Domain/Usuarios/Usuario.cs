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

    private readonly List<DespesaRecorrente> _despesasRecorrentes = [];
    public IReadOnlyList<DespesaRecorrente> DespesasRecorrentes => _despesasRecorrentes;

    private readonly List<Cartao> _cartoes = [];
    public IReadOnlyList<Cartao> Cartoes => _cartoes;

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

    // Falha única: mapeada para 409 em AdicionarCartaoEndpoint. Nova falha aqui exige erro tipado.
    public Result AdicionarCartao(Cartao cartao)
    {
        if (_cartoes.Exists(c => c.Banco == cartao.Banco && c.Sufixo == cartao.Sufixo))
            return Result.Failure($"O usuário já tem um cartão {cartao.Banco.Name} com final {cartao.Sufixo}.");

        _cartoes.Add(cartao);
        return Result.Success();
    }

    public Result AdicionarDespesaRecorrente(DespesaRecorrente despesaRecorrente)
    {
        if (_despesasRecorrentes.Contains(despesaRecorrente))
            return Result.Failure("Despesa recorrente já existe para este usuário.");

        if (despesaRecorrente.IdCartao is IdCartao idCartao && !_cartoes.Exists(c => c.Id == idCartao))
            return Result.Failure("O cartão da despesa recorrente não pertence ao usuário.");

        _despesasRecorrentes.Add(despesaRecorrente);
        return Result.Success();
    }
}
