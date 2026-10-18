namespace ControleFinanceiro.Domain.Shared.ValueObjects;

public sealed class TextoObrigatorio : SimpleValueObject<string>
{
    private TextoObrigatorio(string value) : base(value)
    {
    }

    public static Result<TextoObrigatorio> Criar(string? value, string nomeCampo = "Texto")
    {
        return string.IsNullOrWhiteSpace(value)
            ? Result.Failure<TextoObrigatorio>($"{nomeCampo} é obrigatório")
            : new TextoObrigatorio(value.Trim());
    }

    public static explicit operator TextoObrigatorio(string? value) => Criar(value, "Texto").Value;
}
