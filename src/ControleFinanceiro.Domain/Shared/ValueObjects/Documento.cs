namespace ControleFinanceiro.Domain.Shared.ValueObjects;

/// <summary>
/// O CPF ou o CNPJ, guardado sem a máscara e em maiúsculas. Só existe com os dígitos verificadores
/// conferindo.
/// </summary>
/// <remarks>
/// As mensagens de falha não repetem o número: um documento é dado pessoal, e a mensagem pode ir para um log
/// ou para uma resposta.
/// </remarks>
public abstract class Documento : SimpleValueObject<string>
{
    public const int QuantidadeCaracteresCpf = 11;
    public const int QuantidadeCaracteresCnpj = 14;

    private const int QuantidadeVerificadores = 2;

    private protected Documento(string value) : base(value)
    {
    }

    /// <summary>Escolhe entre o CPF e o CNPJ pela quantidade de letras e dígitos.</summary>
    public static Result<Documento> Criar(string? numero)
    {
        Alfanumerico caracteres = Alfanumerico.Criar(numero);

        return caracteres.Value.Length switch
        {
            QuantidadeCaracteresCpf => Cpf.Criar(caracteres).Map(cpf => (Documento)cpf),
            QuantidadeCaracteresCnpj => Cnpj.Criar(caracteres).Map(cnpj => (Documento)cnpj),
            _ => Result.Failure<Documento>("O documento não é um CPF nem um CNPJ válido"),
        };
    }

    public static explicit operator Documento(string? numero) => Criar(numero).Value;

    private protected static bool TodosIguais(string valor) => valor.All(c => c == valor[0]);

    /// <summary>Confere os dois últimos caracteres, que são os dígitos verificadores da base antes deles.</summary>
    /// <param name="valor">Já sem a máscara, com a base em dígitos ou letras maiúsculas.</param>
    /// <param name="pesoMaximo">O peso a partir do qual os pesos recomeçam em 2.</param>
    private protected static bool VerificadoresConferem(string valor, int pesoMaximo)
    {
        ReadOnlySpan<char> verificadores = valor.AsSpan(valor.Length - QuantidadeVerificadores);
        if (!char.IsAsciiDigit(verificadores[0]) || !char.IsAsciiDigit(verificadores[1]))
            return false;

        return verificadores[0] - '0' == Verificador(valor.AsSpan(start: 0, valor.Length - QuantidadeVerificadores), pesoMaximo)
            && verificadores[1] - '0' == Verificador(valor.AsSpan(start: 0, valor.Length - 1), pesoMaximo);
    }

    /// <summary>
    /// Módulo 11: cada caractere vale o código ASCII menos 48 (<c>0</c>–<c>9</c> valem 0–9, e <c>A</c>–<c>Z</c>
    /// valem 17–42) e é multiplicado por um peso que começa em 2 no último caractere e cresce para a esquerda até o
    /// <paramref name="pesoMaximo"/>, quando recomeça em 2. O verificador é 11 menos o resto da soma por 11, ou 0
    /// quando o resto é menor que 2.
    /// </summary>
    private static int Verificador(ReadOnlySpan<char> caracteres, int pesoMaximo)
    {
        int soma = 0;
        int peso = 2;

        for (int i = caracteres.Length - 1; i >= 0; i--)
        {
            soma += (caracteres[i] - '0') * peso;
            peso = peso == pesoMaximo ? 2 : peso + 1;
        }

        int resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
