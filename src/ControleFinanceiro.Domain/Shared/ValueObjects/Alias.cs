namespace ControleFinanceiro.Domain.Shared.ValueObjects;

/// <summary>
/// Um texto que vem de um arquivo importado, como o nome do beneficiário de um Pix ou o estabelecimento de uma
/// compra, para ser reconhecido de novo no próximo arquivo. Guarda o <see cref="TextoOriginal"/>, para exibir, e o
/// <see cref="TextoNormalizado"/>, para comparar: aparado, com os espaços repetidos juntos, sem acentos e em
/// maiúsculas. Dois aliases são iguais quando o normalizado é o mesmo.
/// </summary>
public sealed class Alias : ValueObject
{
    public string TextoOriginal { get; }
    public string TextoNormalizado { get; }

    private Alias(string textoOriginal, string textoNormalizado)
    {
        TextoOriginal = textoOriginal;
        TextoNormalizado = textoNormalizado;
    }

    /// <remarks>Falha também com o texto que fica vazio depois de normalizado, como o feito só de acentos soltos.</remarks>
    public static Result<Alias> Criar(string? texto)
    {
        string normalizado = ValueObjects.TextoNormalizado.Criar(texto);
        return normalizado.Length == 0
            ? Result.Failure<Alias>("Alias é obrigatório")
            : new Alias(texto!.Trim(), normalizado);
    }

    public static explicit operator Alias(string? texto) => Criar(texto).Value;

    public override string ToString() => TextoOriginal;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return TextoNormalizado;
    }
}
