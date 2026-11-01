using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Transacoes;

/// <summary>
/// Uma linha do extrato ou da fatura, como o adaptador do layout a leu, antes de virar transação.
/// </summary>
/// <remarks>
/// Duas linhas com os mesmos campos são iguais como valor. Quem as distingue é a posição no lote, na
/// <see cref="ChaveDeduplicacao"/>, e não o objeto.
/// </remarks>
public sealed class Lancamento : ValueObject
{
    public DateOnly Data { get; }

    /// <summary>Preenchida quando o layout traz a hora.</summary>
    public TimeOnly? Hora { get; }

    /// <summary>Com sinal: negativo é saída. Zero é aceito.</summary>
    public Reais Valor { get; }

    /// <summary>Verdadeiro quando o dinheiro saiu, ou seja, quando o <see cref="Valor"/> é negativo.</summary>
    public bool EhSaida => Valor < 0;

    /// <summary>
    /// Verdadeiro quando o dinheiro entrou, ou seja, quando o <see cref="Valor"/> é positivo. O valor zero não é
    /// entrada nem saída.
    /// </summary>
    public bool EhEntrada => Valor > 0;

    public TextoObrigatorio Descricao { get; }

    /// <summary>Aparada; nula quando o layout não traz ou traz em branco.</summary>
    public string? Contraparte { get; }

    /// <summary>Preenchida quando o layout traz a parcela; nula na compra à vista e nos layouts sem parcela.</summary>
    public Parcela? Parcela { get; }

    /// <summary>
    /// A categoria que o banco deu à compra, aparada; nula quando o layout não traz ou traz em branco. Não entra
    /// na <see cref="Identidade"/>.
    /// </summary>
    public string? CategoriaBanco { get; }

    /// <summary>Os campos que o layout declara para reconhecer o lançamento numa reimportação.</summary>
    public IdentidadeLancamento Identidade { get; }

    /// <summary>De onde o lançamento veio: o extrato de uma conta ou a fatura de um cartão.</summary>
    public TipoOrigem TipoOrigem { get; }

    private Lancamento(
        DateOnly data,
        TimeOnly? hora,
        Reais valor,
        TextoObrigatorio descricao,
        string? contraparte,
        Parcela? parcela,
        string? categoriaBanco,
        IdentidadeLancamento identidade,
        TipoOrigem tipoOrigem)
    {
        Data = data;
        Hora = hora;
        Valor = valor;
        Descricao = descricao;
        Contraparte = Aparar(contraparte);
        Parcela = parcela;
        CategoriaBanco = Aparar(categoriaBanco);
        Identidade = identidade;
        TipoOrigem = tipoOrigem;
    }

    /// <summary>Uma linha do extrato de uma conta, que nunca traz parcela.</summary>
    public static Lancamento DaConta(
        DateOnly data,
        TimeOnly? hora,
        Reais valor,
        TextoObrigatorio descricao,
        string? contraparte,
        string? categoriaBanco,
        IdentidadeLancamento identidade)
    {
        return new Lancamento(data, hora, valor, descricao, contraparte, parcela: null, categoriaBanco, identidade, TipoOrigem.Conta);
    }

    /// <summary>Uma linha da fatura de um cartão.</summary>
    public static Lancamento DoCartao(
        DateOnly data,
        TimeOnly? hora,
        Reais valor,
        TextoObrigatorio descricao,
        string? contraparte,
        Parcela? parcela,
        string? categoriaBanco,
        IdentidadeLancamento identidade)
    {
        return new Lancamento(data, hora, valor, descricao, contraparte, parcela, categoriaBanco, identidade, TipoOrigem.Cartao);
    }

    /// <summary>
    /// O texto do arquivo que diz quem recebeu o dinheiro de uma saída: a contraparte no extrato da conta (nula em
    /// boleto e tarifa) e a descrição, o estabelecimento, na fatura do cartão. Nulo na entrada, o que inclui o
    /// estorno e o pagamento da fatura do lado do cartão, e no valor zero. A saída da conta para outra conta do
    /// titular, como o pagamento da fatura do lado da conta, tem texto como qualquer saída.
    /// </summary>
    public string? TextoBeneficiario => TextoBeneficiarioDe(EhSaida, Descricao, Contraparte, TipoOrigem);

    /// <summary>A regra do <see cref="TextoBeneficiario"/>, também para a transação, que não guarda o lançamento.</summary>
    internal static string? TextoBeneficiarioDe(bool ehSaida, TextoObrigatorio descricao, string? contraparte, TipoOrigem tipoOrigem)
    {
        if (!ehSaida)
            return null;

        return tipoOrigem switch
        {
            TipoOrigem.Conta => contraparte,
            TipoOrigem.Cartao => descricao,
            _ => throw new ArgumentOutOfRangeException(nameof(tipoOrigem), tipoOrigem, message: null),
        };
    }

    /// <summary>O alias do <see cref="TextoBeneficiario"/>; nulo quando não há texto ou ele fica vazio depois de normalizado.</summary>
    public Alias? AliasTextoBeneficiario => AliasDe(TextoBeneficiario);

    /// <summary>O alias da <see cref="CategoriaBanco"/>; nulo quando não há categoria ou ela fica vazia depois de normalizada.</summary>
    public Alias? AliasCategoriaBanco => AliasDe(CategoriaBanco);

    /// <summary>
    /// Verdadeiro na saída com parcela, que só a fatura traz. A linha sem texto de beneficiário fica de fora porque a despesa exige
    /// um beneficiário.
    /// </summary>
    public bool EhCompraParcelada => EhSaida && Parcela is not null && AliasTextoBeneficiario is not null;

    /// <summary>O <see cref="Valor"/> sem o sinal.</summary>
    public ReaisPositivo ValorAbsoluto => (ReaisPositivo)decimal.Abs(Valor);

    private static Alias? AliasDe(string? texto)
    {
        Result<Alias> alias = Alias.Criar(texto);
        return alias.IsSuccess ? alias.Value : null;
    }

    private static string? Aparar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Data;
        yield return Hora;
        yield return Valor;
        yield return Descricao;
        yield return Contraparte;
        yield return Parcela;
        yield return CategoriaBanco;
        yield return Identidade;
        yield return TipoOrigem;
    }
}
