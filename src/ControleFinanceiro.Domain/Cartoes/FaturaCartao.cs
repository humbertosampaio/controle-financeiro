using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Cartoes;

/// <summary>
/// A fatura de um mês de vencimento: os cartões que saem nela, pela regra do <see cref="Cartao.EhDaMesmaFaturaQue"/>,
/// como os finais do C6, e o mês e o dia em que ela vence, que são os mesmos para todos eles.
/// </summary>
public sealed class FaturaCartao : ValueObject
{
    private readonly IReadOnlySet<IdCartao> _cartoes;

    public IReadOnlyCollection<IdCartao> Cartoes => _cartoes;
    public MesReferencia MesVencimento { get; }

    /// <summary>O dia de vencimento dos cartões da fatura. Fica fora da igualdade: os mesmos cartões têm o mesmo dia.</summary>
    public DiaVencimentoCartao DiaVencimento { get; }

    private FaturaCartao(IReadOnlySet<IdCartao> cartoes, MesReferencia mesVencimento, DiaVencimentoCartao diaVencimento)
    {
        _cartoes = cartoes;
        MesVencimento = mesVencimento;
        DiaVencimento = diaVencimento;
    }

    /// <summary>A fatura do cartão no mês, com os cartões do usuário que saem nela e o próprio cartão.</summary>
    public static FaturaCartao Do(Cartao cartao, IEnumerable<Cartao> cartoesDoUsuario, MesReferencia mesVencimento)
    {
        HashSet<IdCartao> cartoes = cartoesDoUsuario
            .Where(c => c.EhDaMesmaFaturaQue(cartao))
            .Select(c => c.Id)
            .Append(cartao.Id)
            .ToHashSet();

        return new FaturaCartao(cartoes, mesVencimento, cartao.DiaVencimento);
    }

    /// <summary>A fatura do cartão de id informado no mês, como no <see cref="Do"/>.</summary>
    /// <exception cref="ArgumentException">Quando o cartão não está na lista: é bug de quem chama.</exception>
    public static FaturaCartao DoCartao(IdCartao idCartao, IReadOnlyCollection<Cartao> cartoesDoUsuario, MesReferencia mesVencimento) =>
        Do(CartaoComId(idCartao, cartoesDoUsuario), cartoesDoUsuario, mesVencimento);

    /// <summary>A fatura em que cai a compra feita no cartão na data, pelo <see cref="Cartao.MesVencimento"/>.</summary>
    /// <exception cref="ArgumentException">Quando o cartão não está na lista: é bug de quem chama.</exception>
    public static FaturaCartao DaCompra(IdCartao idCartao, DateOnly dataCompra, IReadOnlyCollection<Cartao> cartoesDoUsuario)
    {
        Cartao cartao = CartaoComId(idCartao, cartoesDoUsuario);
        return Do(cartao, cartoesDoUsuario, cartao.MesVencimento(dataCompra));
    }

    public bool Contem(IdCartao idCartao) => _cartoes.Contains(idCartao);

    private static Cartao CartaoComId(IdCartao idCartao, IReadOnlyCollection<Cartao> cartoesDoUsuario)
    {
        return cartoesDoUsuario.FirstOrDefault(c => c.Id == idCartao)
            ?? throw new ArgumentException("O cartão não está na lista.", nameof(cartoesDoUsuario));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MesVencimento;

        foreach (IdCartao idCartao in _cartoes.Order())
            yield return idCartao;
    }
}
