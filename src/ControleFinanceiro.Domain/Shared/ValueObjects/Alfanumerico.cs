namespace ControleFinanceiro.Domain.Shared.ValueObjects;

/// <summary>
/// Um texto só com as letras e os dígitos ASCII (<c>A</c>–<c>Z</c>, <c>a</c>–<c>z</c> e <c>0</c>–<c>9</c>) de outro,
/// na mesma ordem e na mesma caixa: tira a máscara de um documento, por exemplo. Não confere o formato; isso é de
/// quem o recebe, como o <see cref="Cpf"/> e o <see cref="Cnpj"/>.
/// </summary>
public sealed class Alfanumerico : SimpleValueObject<string>
{
    private Alfanumerico(string value) : base(value)
    {
    }

    /// <param name="texto">Nulo vira um texto vazio.</param>
    public static Alfanumerico Criar(string? texto)
    {
        return new Alfanumerico(texto is null ? string.Empty : string.Concat(texto.Where(char.IsAsciiLetterOrDigit)));
    }
}
