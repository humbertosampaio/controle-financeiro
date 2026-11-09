using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Cartoes;

/// <summary>
/// A fatura de um mês de vencimento: os cartões que saem nela, pela regra do <see cref="Cartao.EhDaMesmaFaturaQue"/>,
/// como os finais do C6, e o vencimento, que é o mesmo para todos eles.
/// </summary>
public sealed class FaturaCartao : ValueObject
{
    private readonly IReadOnlySet<IdCartao> _cartoes;

    public IReadOnlyCollection<IdCartao> Cartoes => _cartoes;
    public VencimentoFatura Vencimento { get; }

    private FaturaCartao(IReadOnlySet<IdCartao> cartoes, VencimentoFatura vencimento)
    {
        _cartoes = cartoes;
        Vencimento = vencimento;
    }

    /// <summary>A fatura do cartão no mês, com os cartões do usuário que saem nela e o próprio cartão.</summary>
    public static FaturaCartao DoCartao(Cartao cartao, IEnumerable<Cartao> cartoesDoUsuario, MesReferencia mesVencimento) =>
        DoCartao(cartao, cartoesDoUsuario, cartao.VencimentoNoMes(mesVencimento));

    private static FaturaCartao DoCartao(Cartao cartao, IEnumerable<Cartao> cartoesDoUsuario, VencimentoFatura vencimento)
    {
        HashSet<IdCartao> cartoes = cartoesDoUsuario
            .Where(c => c.EhDaMesmaFaturaQue(cartao))
            .Select(c => c.Id)
            .Append(cartao.Id)
            .ToHashSet();

        return new FaturaCartao(cartoes, vencimento);
    }

    /// <summary>A fatura do cartão de id informado no mês, como no <see cref="DoCartao(Cartao, IEnumerable{Cartao}, MesReferencia)"/>.</summary>
    /// <exception cref="ArgumentException">Quando o cartão não está na lista: é bug de quem chama.</exception>
    public static FaturaCartao DoCartao(IdCartao idCartao, IReadOnlyCollection<Cartao> cartoesDoUsuario, MesReferencia mesVencimento) =>
        DoCartao(CartaoComId(idCartao, cartoesDoUsuario), cartoesDoUsuario, mesVencimento);

    /// <summary>A fatura em que cai a compra feita no cartão na data, pelo <see cref="Cartao.VencimentoDaCompra"/>.</summary>
    /// <exception cref="ArgumentException">Quando o cartão não está na lista: é bug de quem chama.</exception>
    public static FaturaCartao DaCompra(IdCartao idCartao, DateOnly dataCompra, IReadOnlyCollection<Cartao> cartoesDoUsuario)
    {
        Cartao cartao = CartaoComId(idCartao, cartoesDoUsuario);
        return DoCartao(cartao, cartoesDoUsuario, cartao.VencimentoDaCompra(dataCompra));
    }

    public bool Contem(IdCartao idCartao) => _cartoes.Contains(idCartao);

    private static Cartao CartaoComId(IdCartao idCartao, IReadOnlyCollection<Cartao> cartoesDoUsuario)
    {
        return cartoesDoUsuario.FirstOrDefault(c => c.Id == idCartao)
            ?? throw new ArgumentException("O cartão não está na lista.", nameof(cartoesDoUsuario));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Vencimento;

        foreach (IdCartao idCartao in _cartoes.Order())
            yield return idCartao;
    }
}
