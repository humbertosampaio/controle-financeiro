using System.Text.RegularExpressions;

namespace ControleFinanceiro.Domain.Shared.ValueObjects;

/// <summary>
/// Um texto vindo de um banco sem os documentos que ele trazia: cada CNPJ e cada CPF válidos, com ou sem máscara, e
/// cada CPF mascarado com <c>*</c>, junto com os espaços antes deles, e aparado. O CPF e o CNPJ só de dígitos saem
/// mesmo colados a letras; o CNPJ com letras, só separado delas.
/// </summary>
/// <remarks>
/// Um número com a forma de um documento, mas com os verificadores errados, fica: não é documento, e pode ser parte
/// do texto, como um número de pedido.
/// </remarks>
public sealed partial class TextoSemDocumentos : SimpleValueObject<string>
{
    private TextoSemDocumentos(string value) : base(value)
    {
    }

    /// <param name="texto">Nulo vira um texto vazio.</param>
    public static TextoSemDocumentos Criar(string? texto)
    {
        if (texto is null)
            return new TextoSemDocumentos(string.Empty);

        string semCnpjNumerico = CandidatoCnpjNumerico().Replace(texto, candidato => TirarSe(candidato, numero => Cnpj.Criar(numero).IsSuccess));
        string semCnpj = CandidatoCnpj().Replace(semCnpjNumerico, candidato => TirarSe(candidato, numero => Cnpj.Criar(numero).IsSuccess));
        string semCpf = CandidatoCpf().Replace(semCnpj, candidato => TirarSe(candidato, numero => Cpf.Criar(numero).IsSuccess));
        string semCpfMascarado = CandidatoCpfMascarado().Replace(semCpf, candidato => TirarSe(candidato, numero => numero.Contains('*')));

        return new TextoSemDocumentos(semCpfMascarado.Trim());
    }

    public static explicit operator TextoSemDocumentos(string? texto) => Criar(texto);

    /// <returns>Vazio quando o documento do <paramref name="candidato"/> é tirado; senão, o candidato como veio.</returns>
    private static string TirarSe(Match candidato, Func<string, bool> tirar)
    {
        return tirar(candidato.Groups["documento"].Value) ? string.Empty : candidato.Value;
    }

    // [0-9], e não \d, que aceita dígitos de outros alfabetos. Os separadores entre os grupos são opcionais.
    [GeneratedRegex(@"\s*(?<![0-9])(?<documento>[0-9]{3}[. ]?[0-9]{3}[. ]?[0-9]{3}[-. ]?[0-9]{2})(?![0-9])")]
    private static partial Regex CandidatoCpf();

    [GeneratedRegex(@"\s*(?<![0-9*])(?<documento>[0-9*]{3}\.?[0-9*]{3}\.?[0-9*]{3}-?[0-9*]{2})(?![0-9*])")]
    private static partial Regex CandidatoCpfMascarado();

    [GeneratedRegex(@"\s*(?<![0-9])(?<documento>[0-9]{2}[. ]?[0-9]{3}[. ]?[0-9]{3}[/ ]?[0-9]{4}[- ]?[0-9]{2})(?![0-9])")]
    private static partial Regex CandidatoCnpjNumerico();

    // As 12 primeiras posições do CNPJ podem ter letras; os verificadores são sempre dígitos. Sem espaço entre os grupos:
    // com letras, ele faria trechos de nomes virarem candidatos.
    [GeneratedRegex(@"\s*(?<![0-9A-Za-z])(?<documento>[0-9A-Za-z]{2}\.?[0-9A-Za-z]{3}\.?[0-9A-Za-z]{3}/?[0-9A-Za-z]{4}-?[0-9]{2})(?![0-9A-Za-z])")]
    private static partial Regex CandidatoCnpj();
}
