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

    private Lancamento(
        DateOnly data,
        TimeOnly? hora,
        Reais valor,
        TextoObrigatorio descricao,
        string? contraparte,
        Parcela? parcela,
        string? categoriaBanco,
        IdentidadeLancamento identidade)
    {
        Data = data;
        Hora = hora;
        Valor = valor;
        Descricao = descricao;
        Contraparte = contraparte;
        Parcela = parcela;
        CategoriaBanco = categoriaBanco;
        Identidade = identidade;
    }

    public static Lancamento Criar(
        DateOnly data,
        TimeOnly? hora,
        Reais valor,
        TextoObrigatorio descricao,
        string? contraparte,
        Parcela? parcela,
        string? categoriaBanco,
        IdentidadeLancamento identidade)
    {
        return new Lancamento(data, hora, valor, descricao, Aparar(contraparte), parcela, Aparar(categoriaBanco), identidade);
    }

    /// <summary>
    /// O texto do arquivo que diz quem recebeu o dinheiro de uma saída: a contraparte no extrato da conta (nula em
    /// boleto e tarifa) e a descrição, o estabelecimento, na fatura do cartão. Nulo na entrada, o que inclui o
    /// estorno e o pagamento da fatura do lado do cartão, e no valor zero. A saída da conta para outra conta do titular, como o
    /// pagamento da fatura do lado da conta, tem texto como qualquer saída.
    /// </summary>
    public string? TextoBeneficiario(TipoOrigem tipoOrigem) => TextoBeneficiarioDe(EhSaida, Descricao, Contraparte, tipoOrigem);

    /// <summary>A regra do <see cref="TextoBeneficiario"/>, também para a transação, que não guarda o lançamento.</summary>
    internal static string? TextoBeneficiarioDe(bool ehSaida, TextoObrigatorio descricao, string? contraparte, TipoOrigem tipoOrigem)
    {
        if (ehSaida)
        {
            return tipoOrigem switch
            {
                TipoOrigem.Conta => contraparte,
                TipoOrigem.Cartao => descricao,
                _ => throw new ArgumentOutOfRangeException(nameof(tipoOrigem), tipoOrigem, message: null),
            };
        }

        return null;
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
    }
}
