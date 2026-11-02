using System.Text.RegularExpressions;

namespace ControleFinanceiro.Domain.Shared.ValueObjects;

/// <summary>
/// Um texto vindo de um banco, aparado e com cada CPF e CNPJ de verificadores válidos, com ou sem pontuação, trocado por
/// <c>[CPF]</c> ou <c>[CNPJ]</c>.
/// </summary>
/// <remarks>
/// Reduz a exposição de documentos de terceiros, sem garantir que nenhum saia: uma regra mais larga apagaria datas,
/// pedidos e valores, e ainda deixaria escapar outras formas. Ficam no texto, de propósito: número com verificador
/// errado, CPF mascarado com <c>*</c>, documento com outro separador, CNPJ com letras colado a outras letras, conta,
/// agência, telefone, e-mail e nomes. O que não pode sair de jeito nenhum não entra nos tipos de saída.
/// </remarks>
public sealed partial class DocumentoMascarado : SimpleValueObject<string>
{
    private const string RotuloCpf = "[CPF]";
    private const string RotuloCnpj = "[CNPJ]";

    private DocumentoMascarado(string value) : base(value)
    {
    }

    /// <summary>Se sobrou algo além dos rótulos, de espaços e de separadores.</summary>
    public bool TemConteudo => Rotulo().Replace(Value, string.Empty).Any(char.IsLetterOrDigit);

    /// <summary>O texto, ou vazio quando só sobraram rótulos: um rótulo sozinho não diz nada a quem lê.</summary>
    public string ConteudoOuVazio => TemConteudo ? Value : string.Empty;

    /// <param name="texto">Nulo vira um texto vazio.</param>
    public static DocumentoMascarado Criar(string? texto)
    {
        if (texto is null)
            return new DocumentoMascarado(string.Empty);

        string semCnpjNumerico = CandidatoCnpjNumerico().Replace(texto, candidato => RotuloCnpjSeValido(candidato));
        string semCnpj = CandidatoCnpjComLetras().Replace(semCnpjNumerico, candidato => RotuloCnpjSeValido(candidato));
        string semCpf = CandidatoCpf().Replace(semCnpj, candidato => Cpf.Criar(candidato.Value).IsSuccess ? RotuloCpf : candidato.Value);

        return new DocumentoMascarado(semCpf.Trim());
    }

    public static explicit operator DocumentoMascarado(string? texto) => Criar(texto);

    public static explicit operator DocumentoMascarado(TextoObrigatorio texto) => Criar(texto);

    private static string RotuloCnpjSeValido(Match candidato) => Cnpj.Criar(candidato.Value).IsSuccess ? RotuloCnpj : candidato.Value;

    [GeneratedRegex(@"\[(?:CPF|CNPJ)\]")]
    private static partial Regex Rotulo();

    // [0-9], e não \d, que aceita dígitos de outros alfabetos. Os separadores entre os grupos são opcionais.
    [GeneratedRegex(@"(?<![0-9])[0-9]{3}[. ]?[0-9]{3}[. ]?[0-9]{3}[-. ]?[0-9]{2}(?![0-9])")]
    private static partial Regex CandidatoCpf();

    [GeneratedRegex(@"(?<![0-9])[0-9]{2}[. ]?[0-9]{3}[. ]?[0-9]{3}[/ ]?[0-9]{4}[- ]?[0-9]{2}(?![0-9])")]
    private static partial Regex CandidatoCnpjNumerico();

    // As 12 primeiras posições do CNPJ podem ter letras; os verificadores são sempre dígitos. Sem espaço entre os grupos:
    // com letras, ele faria trechos de nomes virarem candidatos.
    [GeneratedRegex(@"(?<![0-9A-Za-z])[0-9A-Za-z]{2}\.?[0-9A-Za-z]{3}\.?[0-9A-Za-z]{3}/?[0-9A-Za-z]{4}-?[0-9]{2}(?![0-9A-Za-z])")]
    private static partial Regex CandidatoCnpjComLetras();
}
