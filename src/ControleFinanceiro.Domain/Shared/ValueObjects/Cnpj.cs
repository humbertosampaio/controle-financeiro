namespace ControleFinanceiro.Domain.Shared.ValueObjects;

/// <summary>
/// O CNPJ na regra vigente desde julho de 2026 (IN RFB 2.229/2024): 12 caracteres alfanuméricos (<c>0</c>–<c>9</c> e
/// <c>A</c>–<c>Z</c>) e 2 dígitos verificadores, sem todos os caracteres iguais. Os pesos vão de 2 a 9, da direita
/// para a esquerda, recomeçando em 2. O CNPJ só com dígitos é um caso particular e continua válido.
/// </summary>
public sealed class Cnpj : Documento
{
    private const int PesoMaximo = 9;

    private Cnpj(string value) : base(value)
    {
    }

    /// <param name="numero">Com ou sem a máscara <c>00.000.000/0000-00</c>.</param>
    public static new Result<Cnpj> Criar(string? numero) => Criar(Alfanumerico.Criar(numero));

    /// <param name="numero">Letras minúsculas viram maiúsculas.</param>
    public static Result<Cnpj> Criar(Alfanumerico numero)
    {
        string valor = numero.Value.ToUpperInvariant();

        return Result
            .SuccessIf(
                valor.Length == QuantidadeCaracteresCnpj
                    && valor.All(c => char.IsAsciiDigit(c) || char.IsAsciiLetterUpper(c))
                    && !TodosIguais(valor)
                    && VerificadoresConferem(valor, PesoMaximo),
                valor,
                "O documento não é um CNPJ válido")
            .Map(v => new Cnpj(v));
    }

    public static explicit operator Cnpj(string? numero) => Criar(numero).Value;
}
