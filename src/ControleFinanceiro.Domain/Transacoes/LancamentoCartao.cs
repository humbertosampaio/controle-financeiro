using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Transacoes;

/// <summary>Uma linha da fatura de um cartão: a compra, à vista ou parcelada, o estorno, a tarifa ou o pagamento da fatura anterior.</summary>
public sealed class LancamentoCartao : Lancamento
{
    /// <summary>Preenchida quando o layout traz a parcela; nula na compra à vista e nos layouts sem parcela.</summary>
    public Parcela? Parcela { get; }

    /// <summary>Verdadeiro quando o lançamento não traz parcela: a compra à vista, ou a entrada.</summary>
    public bool EhAVista => Parcela is null;

    /// <summary>
    /// O primeiro mês em que este lançamento pode vencer: o mês da <see cref="Lancamento.Data"/>, avançado um mês por
    /// parcela anterior a ele. A parcela traz a data da compra original, e cada parcela anterior venceu numa fatura antes
    /// desta; o lançamento à vista não avança.
    /// </summary>
    public MesReferencia PrimeiroMesVencimentoPossivel => Data.AddMonths(Parcela?.QuantidadeParcelasAnteriores ?? 0);

    /// <summary>
    /// Verdadeiro na linha que é o pagamento da fatura anterior, que só nasce pelo <see cref="PagamentoFaturaAnterior"/>. Não entra
    /// na <see cref="Lancamento.Identidade"/>: reimportar a linha não muda a chave dela.
    /// </summary>
    public bool EhPagamentoFaturaAnterior { get; }

    private LancamentoCartao(
        DateOnly data,
        TimeOnly? hora,
        Reais valor,
        TextoObrigatorio descricao,
        string? contraparte,
        Parcela? parcela,
        string? categoriaBanco,
        IdentidadeLancamento identidade,
        bool ehPagamentoFaturaAnterior)
            : base(data, hora, valor, descricao, contraparte, categoriaBanco, identidade)
    {
        Parcela = parcela;
        EhPagamentoFaturaAnterior = ehPagamentoFaturaAnterior;
    }

    public static LancamentoCartao Criar(
        DateOnly data,
        TimeOnly? hora,
        Reais valor,
        TextoObrigatorio descricao,
        string? contraparte,
        Parcela? parcela,
        string? categoriaBanco,
        IdentidadeLancamento identidade)
    {
        return new LancamentoCartao(data, hora, valor, descricao, contraparte, parcela, categoriaBanco, identidade, ehPagamentoFaturaAnterior: false);
    }

    /// <summary>
    /// A linha que é o pagamento da fatura anterior: entra no cartão com valor não negativo e sem parcela. Continua sendo
    /// uma entrada no cartão e não paga despesa; quem paga a fatura é a saída da conta.
    /// </summary>
    public static LancamentoCartao PagamentoFaturaAnterior(
        DateOnly data,
        TimeOnly? hora,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        string? contraparte,
        string? categoriaBanco,
        IdentidadeLancamento identidade)
    {
        // O valor vai como Reais, e não como o ReaisPositivo recebido: a igualdade dos value objects compara o tipo.
        return new LancamentoCartao(data, hora, valor.Value, descricao, contraparte, parcela: null, categoriaBanco, identidade, ehPagamentoFaturaAnterior: true);
    }

    /// <summary>
    /// A descrição, o estabelecimento, na saída. Nulo na entrada, o que inclui o estorno e o pagamento da fatura.
    /// </summary>
    public override string? TextoBeneficiario => TextoBeneficiarioDe(EhSaida, Descricao);

    /// <summary>A regra do <see cref="TextoBeneficiario"/>, também para a transação, que não guarda o lançamento.</summary>
    internal static string? TextoBeneficiarioDe(bool ehSaida, TextoObrigatorio descricao) => ehSaida ? descricao : null;

    /// <summary>
    /// Verdadeiro na saída com parcela. A linha sem texto de beneficiário fica de fora porque a despesa exige um beneficiário.
    /// </summary>
    public bool EhCompraParcelada => EhSaida && Parcela is not null && AliasTextoBeneficiario is not null;

    protected override IEnumerable<object?> ComponentesIgualdadeOrigem()
    {
        yield return Parcela;
        yield return EhPagamentoFaturaAnterior;
    }
}
