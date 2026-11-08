using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Cartoes;

/// <summary>
/// A fatura de um mês de vencimento: os cartões que saem nela, pela regra do <see cref="Cartao.EhDaMesmaFaturaQue"/>,
/// como os finais do C6, e o mês em que ela vence.
/// </summary>
public sealed class FaturaCartao : ValueObject
{
    private readonly IReadOnlySet<IdCartao> _cartoes;

    public IReadOnlyCollection<IdCartao> Cartoes => _cartoes;
    public MesReferencia MesVencimento { get; }

    private FaturaCartao(IReadOnlySet<IdCartao> cartoes, MesReferencia mesVencimento)
    {
        _cartoes = cartoes;
        MesVencimento = mesVencimento;
    }

    /// <summary>A fatura do cartão no mês, com os cartões do usuário que saem nela e o próprio cartão.</summary>
    public static FaturaCartao Do(Cartao cartao, IEnumerable<Cartao> cartoesDoUsuario, MesReferencia mesVencimento)
    {
        HashSet<IdCartao> cartoes = cartoesDoUsuario
            .Where(c => c.EhDaMesmaFaturaQue(cartao))
            .Select(c => c.Id)
            .Append(cartao.Id)
            .ToHashSet();

        return new FaturaCartao(cartoes, mesVencimento);
    }

    public bool Contem(IdCartao idCartao) => _cartoes.Contains(idCartao);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MesVencimento;

        foreach (IdCartao idCartao in _cartoes.Order())
            yield return idCartao;
    }
}
