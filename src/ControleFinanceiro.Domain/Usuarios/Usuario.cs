using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Contas;
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

    private readonly List<Cartao> _cartoes = [];
    public IReadOnlyList<Cartao> Cartoes => _cartoes;

    private readonly List<ContaBancaria> _contas = [];
    public IReadOnlyList<ContaBancaria> Contas => _contas;

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

    public Result AdicionarConta(ContaBancaria conta)
    {
        if (_contas.Exists(c => c.Banco == conta.Banco && c.Sufixo == conta.Sufixo))
            return Result.Failure($"O usuário já tem uma conta {conta.Banco.Name} com final {conta.Sufixo}.");

        _contas.Add(conta);
        return Result.Success();
    }

    /// <summary>
    /// A conta de onde veio um extrato, casada por banco e sufixo. Sem uma, cria a conta com o apelido padrão.
    /// </summary>
    public Result<ContaBancaria> ObterOuAdicionarConta(Banco banco, TipoConta tipo, SufixoConta sufixo)
    {
        ContaBancaria? existente = _contas.Find(c => c.Banco == banco && c.Sufixo == sufixo);
        if (existente is not null)
        {
            return existente.Tipo == tipo
                ? existente
                : Result.Failure<ContaBancaria>(
                    $"O usuário já tem uma conta {banco.Name} com final {sufixo}, mas do tipo {existente.Tipo.Name}.");
        }

        ContaBancaria conta = ContaBancaria.Criar(Id, banco, tipo, sufixo);
        _contas.Add(conta);
        return conta;
    }
}
