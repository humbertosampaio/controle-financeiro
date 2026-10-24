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
