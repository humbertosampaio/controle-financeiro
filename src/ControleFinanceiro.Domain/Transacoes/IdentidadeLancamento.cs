using ControleFinanceiro.Domain.Shared.ValueObjects;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ControleFinanceiro.Domain.Transacoes;

/// <summary>
/// Os campos que identificam um lançamento entre importações, na ordem em que o layout os declara. O
/// adaptador de cada layout escolhe quais campos entram; a formatação de cada um é daqui, para que a
/// mesma data ou o mesmo texto vire sempre o mesmo campo, qualquer que seja o banco.
/// </summary>
/// <remarks>
/// <para>
/// A identidade é única quando o banco dá ao lançamento um identificador que basta sozinho. Senão, ela é
/// composta: começa pela data, quando o banco não dá identificador, ou por um identificador do banco que não
/// basta sozinho, como o número que se repete em todas as parcelas da mesma compra.
/// </para>
/// <para>
/// Mudar a formatação de um campo muda as chaves já gravadas: exige uma versão nova da
/// <see cref="ChaveDeduplicacao"/>.
/// </para>
/// <para>
/// A formatação usa a cultura invariante de propósito, e não a <see cref="Shared.AppDefaults.CulturaPtBr"/>: o texto
/// precisa sair igual em qualquer máquina, e a pt-BR muda com as configurações regionais do usuário e
/// com a versão do ICU, além de não existir no modo de globalização invariante.
/// </para>
/// </remarks>
public sealed partial class IdentidadeLancamento : ValueObject
{
    private readonly string[] _campos;

    /// <summary>Os campos já formatados, na ordem declarada.</summary>
    public IReadOnlyList<string> Campos => _campos;

    /// <summary>
    /// Verdadeiro quando o banco dá um identificador próprio ao lançamento: ele não pode se repetir no lote.
    /// </summary>
    public bool EhUnica { get; }

    private IdentidadeLancamento(string[] campos, bool ehUnica)
    {
        _campos = campos;
        EhUnica = ehUnica;
    }

    /// <summary>
    /// Identidade pelo identificador que o banco dá ao lançamento, como ele o escreveu, só aparado. Falha
    /// quando a linha traz o identificador em branco.
    /// </summary>
    public static Result<IdentidadeLancamento> Unica(string? idExterno)
    {
        return string.IsNullOrWhiteSpace(idExterno)
            ? Result.Failure<IdentidadeLancamento>("O identificador do lançamento no banco está em branco.")
            : new IdentidadeLancamento([idExterno.Trim()], ehUnica: true);
    }

    /// <summary>
    /// Identidade montada campo a campo, para layouts sem identificador próprio do lançamento. Começa pela
    /// data, no formato <c>yyyy-MM-dd</c>: é ela que faz o ordinal da <see cref="ChaveDeduplicacao"/>
    /// contar os lançamentos iguais do mesmo dia, e não do lote inteiro.
    /// </summary>
    public static IdentidadeLancamento Composta(DateOnly data)
    {
        return new([data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)], ehUnica: false);
    }

    /// <summary>
    /// Identidade montada campo a campo, para layouts em que o identificador do banco não basta sozinho, como o
    /// número que se repete nas parcelas da mesma compra. Começa pelo identificador como o banco o escreveu, só
    /// aparado. Falha quando a linha traz o identificador em branco.
    /// </summary>
    public static Result<IdentidadeLancamento> Composta(string? identificador)
    {
        return string.IsNullOrWhiteSpace(identificador)
            ? Result.Failure<IdentidadeLancamento>("O identificador do lançamento no banco está em branco.")
            : new IdentidadeLancamento([identificador.Trim()], ehUnica: false);
    }

    /// <summary>Hora no formato <c>HH:mm:ss</c>; vazia quando o lançamento não traz.</summary>
    public IdentidadeLancamento ComHora(TimeOnly? hora) => Com(hora?.ToString("HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty);

    /// <summary>Valor com sinal e 2 casas, com ponto decimal.</summary>
    public IdentidadeLancamento ComValor(Reais valor) => Com(valor.Value.ToString("F2", CultureInfo.InvariantCulture));

    /// <summary>
    /// Texto aparado, com os espaços colapsados, os acentos compostos (NFC) e em maiúsculas; vazio quando
    /// o lançamento não traz.
    /// </summary>
    public IdentidadeLancamento ComTexto(string? texto)
    {
        string campo = string.IsNullOrWhiteSpace(texto)
            ? string.Empty
            : Espacos().Replace(texto.Trim(), " ").Normalize(NormalizationForm.FormC).ToUpperInvariant();

        return Com(campo);
    }

    /// <summary>Parcela como <c>n/m</c>; vazia quando o lançamento não tem parcela.</summary>
    public IdentidadeLancamento ComParcela(Parcela? parcela)
    {
        string campo = parcela is null
            ? string.Empty
            : string.Create(CultureInfo.InvariantCulture, $"{parcela.Numero}/{parcela.Total}");

        return Com(campo);
    }

    private IdentidadeLancamento Com(string campo)
    {
        if (EhUnica)
            throw new InvalidOperationException("A identidade única é só o identificador do banco: não recebe outros campos.");

        return new([.. _campos, campo], ehUnica: false);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return EhUnica;

        foreach (string campo in _campos)
            yield return campo;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Espacos();
}
