using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Transacoes;

/// <summary>
/// Uma linha do arquivo, como o adaptador do layout a leu, antes de virar transação: a do extrato de uma conta é o
/// <see cref="LancamentoConta"/>, e a da fatura de um cartão, o <see cref="LancamentoCartao"/>.
/// </summary>
/// <remarks>
/// Duas linhas com os mesmos campos são iguais como valor, e uma linha da conta nunca é igual a uma do cartão. Quem as
/// distingue é a posição no lote, na <see cref="ChaveDeduplicacao"/>, e não o objeto.
/// </remarks>
public abstract class Lancamento : ValueObject
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

    /// <summary>
    /// A categoria que o banco deu à compra, aparada; nula quando o layout não traz ou traz em branco. Não entra
    /// na <see cref="Identidade"/>.
    /// </summary>
    public string? CategoriaBanco { get; }

    /// <summary>Os campos que o layout declara para reconhecer o lançamento numa reimportação.</summary>
    public IdentidadeLancamento Identidade { get; }

    protected Lancamento(
        DateOnly data,
        TimeOnly? hora,
        Reais valor,
        TextoObrigatorio descricao,
        string? contraparte,
        string? categoriaBanco,
        IdentidadeLancamento identidade)
    {
        Data = data;
        Hora = hora;
        Valor = valor;
        Descricao = descricao;
        Contraparte = Aparar(contraparte);
        CategoriaBanco = Aparar(categoriaBanco);
        Identidade = identidade;
    }

    /// <summary>
    /// O texto do arquivo que diz quem recebeu o dinheiro de uma saída. Nulo na entrada e no valor zero. A saída da conta
    /// para outra conta do titular, como o pagamento da fatura do lado da conta, tem texto como qualquer saída.
    /// </summary>
    public abstract string? TextoBeneficiario { get; }

    /// <summary>O alias do <see cref="TextoBeneficiario"/>; nulo quando não há texto ou ele fica vazio depois de normalizado.</summary>
    public Alias? AliasTextoBeneficiario => Alias.DoTextoOuNulo(TextoBeneficiario);

    /// <summary>O alias da <see cref="CategoriaBanco"/>; nulo quando não há categoria ou ela fica vazia depois de normalizada.</summary>
    public Alias? AliasCategoriaBanco => Alias.DoTextoOuNulo(CategoriaBanco);

    /// <summary>O <see cref="Valor"/> sem o sinal.</summary>
    public ReaisPositivo ValorAbsoluto => (ReaisPositivo)decimal.Abs(Valor);

    private static string? Aparar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    /// <summary>Os componentes de igualdade que só a origem da filha tem.</summary>
    protected abstract IEnumerable<object?> ComponentesIgualdadeOrigem();

    protected sealed override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Data;
        yield return Hora;
        yield return Valor;
        yield return Descricao;
        yield return Contraparte;
        yield return CategoriaBanco;
        yield return Identidade;

        foreach (object? componente in ComponentesIgualdadeOrigem())
            yield return componente;
    }
}
