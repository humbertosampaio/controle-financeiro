namespace ControleFinanceiro.Domain.Shared.ValueObjects;

/// <summary>
/// O CPF: 11 dígitos, nem todos iguais, com os 2 últimos sendo os verificadores dos anteriores. Os pesos vão de 2 a
/// 11, da direita para a esquerda, sem recomeçar.
/// </summary>
public sealed class Cpf : Documento
{
    private const int PesoMaximo = 11;

    private Cpf(string value) : base(value)
    {
    }

    /// <param name="numero">Com ou sem a máscara <c>000.000.000-00</c>.</param>
    public static new Result<Cpf> Criar(string? numero) => Criar(Alfanumerico.Criar(numero));

    public static Result<Cpf> Criar(Alfanumerico numero)
    {
        return Result
            .SuccessIf(
                numero.Value.Length == QuantidadeCaracteresCpf
                    && numero.Value.All(char.IsAsciiDigit)
                    && !TodosIguais(numero.Value)
                    && VerificadoresConferem(numero.Value, PesoMaximo),
                numero.Value,
                "O documento não é um CPF válido")
            .Map(v => new Cpf(v));
    }

    public static explicit operator Cpf(string? numero) => Criar(numero).Value;
}
