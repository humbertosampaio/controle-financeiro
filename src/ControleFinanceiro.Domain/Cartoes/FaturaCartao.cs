using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Cartoes;

/// <summary>
/// A fatura de um mês de vencimento: os cartões que saem nela, pela regra do <see cref="Cartao.EhDaMesmaFaturaQue"/>,
/// como os finais do C6, o banco e o vencimento, que são os mesmos para todos eles.
/// </summary>
public sealed class FaturaCartao : ValueObject
{
    private readonly IReadOnlySet<IdCartao> _cartoes;

    public IReadOnlyCollection<IdCartao> Cartoes => _cartoes;
    public Banco Banco { get; }
    public VencimentoFatura Vencimento { get; }

    private FaturaCartao(IReadOnlySet<IdCartao> cartoes, Banco banco, VencimentoFatura vencimento)
    {
        _cartoes = cartoes;
        Banco = banco;
        Vencimento = vencimento;
    }

    /// <summary>
    /// A fatura do cartão de id informado no mês, com os cartões do usuário que constam nela e o vencimento projetado pelo
    /// dia do cartão. O banco e o vencimento são os do cartão, pela regra do <see cref="Cartao.EhDaMesmaFaturaQue"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Quando o cartão não está na lista: é bug de quem chama.</exception>
    public static FaturaCartao DoCartao(IdCartao idCartao, IReadOnlyCollection<Cartao> cartoesUsuario, MesReferencia mesVencimento)
    {
        Cartao cartao = CartaoComId(idCartao, cartoesUsuario);
        return Agrupar(cartao.Banco, [cartao], cartoesUsuario, cartao.VencimentoNoMes(mesVencimento));
    }

    /// <summary>
    /// A fatura de um arquivo importado: os <paramref name="cartoesArquivo"/>, os cartões do usuário que saem na mesma
    /// fatura que algum deles e o vencimento exato do arquivo, e não o projetado pelo ciclo.
    /// </summary>
    /// <param name="banco">O banco da fatura, que o layout do arquivo define, mesmo quando nenhum final tem cartão.</param>
    /// <exception cref="ArgumentException">Quando um cartão do arquivo é de outro banco: é bug de quem chama.</exception>
    public static FaturaCartao Importada(
        Banco banco,
        IReadOnlyCollection<Cartao> cartoesArquivo,
        IReadOnlyCollection<Cartao> cartoesUsuario,
        VencimentoFatura vencimento)
    {
        if (cartoesArquivo.Any(c => c.Banco != banco))
            throw new ArgumentException("A lista traz um cartão de outro banco.", nameof(cartoesArquivo));

        return Agrupar(banco, cartoesArquivo, cartoesUsuario, vencimento);
    }

    /// <summary>As faturas do usuário que vencem no mês, uma por grupo de cartões da mesma fatura, com o vencimento projetado.</summary>
    public static IReadOnlyList<FaturaCartao> TodasVencendoEm(IReadOnlyCollection<Cartao> cartoesUsuario, MesReferencia mesVencimento)
    {
        return cartoesUsuario
            .Select(cartao => DoCartao(cartao.Id, cartoesUsuario, mesVencimento))
            .Distinct()
            .ToList();
    }

    public bool Contem(IdCartao idCartao) => _cartoes.Contains(idCartao);

    /// <summary>As <paramref name="referencias"/> e os cartões do usuário que constam na mesma fatura que alguma delas.</summary>
    private static FaturaCartao Agrupar(
        Banco banco,
        IReadOnlyCollection<Cartao> referencias,
        IReadOnlyCollection<Cartao> cartoesUsuario,
        VencimentoFatura vencimento)
    {
        HashSet<IdCartao> cartoes = cartoesUsuario
            .Where(c => referencias.Any(c.EhDaMesmaFaturaQue))
            .Concat(referencias)
            .Select(c => c.Id)
            .ToHashSet();

        return new FaturaCartao(cartoes, banco, vencimento);
    }

    private static Cartao CartaoComId(IdCartao idCartao, IReadOnlyCollection<Cartao> cartoesUsuario)
    {
        return cartoesUsuario.FirstOrDefault(c => c.Id == idCartao)
            ?? throw new ArgumentException("O cartão não está na lista.", nameof(cartoesUsuario));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Banco;
        yield return Vencimento;

        foreach (IdCartao idCartao in _cartoes.Order())
            yield return idCartao;
    }
}
