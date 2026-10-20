using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ControleFinanceiro.Domain.Transacoes;

/// <summary>
/// Identifica o lançamento de um extrato entre importações, para reimportar um período sem duplicar
/// transações. É o SHA-256, em hex minúsculo, do texto canônico
/// <c>v1|&lt;COMPE&gt;|&lt;tipo de origem&gt;|&lt;id da origem&gt;|&lt;tipo da identidade&gt;|&lt;campos da identidade&gt;|&lt;ordinal&gt;</c>.
/// </summary>
/// <remarks>
/// <para>
/// Os campos vêm da <see cref="IdentidadeLancamento"/>, que o adaptador de cada layout declara. O banco
/// e o tipo de origem no prefixo identificam o layout, então layouts diferentes nunca produzem o mesmo
/// texto. O tipo da identidade (<c>unica</c> ou <c>composta</c>) impede que um id externo e um campo de
/// texto iguais produzam a mesma chave.
/// </para>
/// <para>
/// O ordinal numera de 1 a n os lançamentos de identidade igual no lote: duas compras iguais no mesmo dia
/// continuam sendo duas. Como a identidade composta começa pela data e o extrato traz sempre o dia
/// inteiro, reimportar um período sobreposto gera as mesmas chaves.
/// </para>
/// <para>Mudar o texto canônico exige uma versão nova (<c>v2</c>): as chaves gravadas deixariam de bater.</para>
/// </remarks>
public sealed class ChaveDeduplicacao : SimpleValueObject<string>
{
    public const string Versao = "v1";
    public const int QuantidadeCaracteres = 64;

    private const char Separador = '|';

    private ChaveDeduplicacao(string value) : base(value)
    {
    }

    public static Result<ChaveDeduplicacao> Criar(string? chave)
    {
        return Result
            .SuccessIf(
                chave is { Length: QuantidadeCaracteres } && chave.All(char.IsAsciiHexDigitLower),
                chave!,
                $"A chave de deduplicação deve ter {QuantidadeCaracteres} caracteres hexadecimais minúsculos")
            .Map(c => new ChaveDeduplicacao(c));
    }

    /// <summary>
    /// Calcula as chaves do lote, na ordem das identidades, numerando as identidades iguais.
    /// </summary>
    public static IReadOnlyList<ChaveDeduplicacao> CalcularLote(
        Banco banco,
        OrigemTransacao origem,
        IEnumerable<IdentidadeLancamento> identidades)
    {
        Dictionary<string, int> ocorrencias = [];
        List<ChaveDeduplicacao> chaves = [];

        foreach (IdentidadeLancamento identidade in identidades)
        {
            string identificacao = Identificacao(banco, origem, identidade);
            int ordinal = ocorrencias.GetValueOrDefault(identificacao) + 1;
            ocorrencias[identificacao] = ordinal;
            chaves.Add(Hash(identificacao, ordinal));
        }

        return chaves;
    }

    /// <param name="ordinal">A posição do lançamento entre os de identidade igual no lote, a partir de 1.</param>
    public static ChaveDeduplicacao Calcular(Banco banco, OrigemTransacao origem, IdentidadeLancamento identidade, int ordinal)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(ordinal, 1);
        return Hash(Identificacao(banco, origem, identidade), ordinal);
    }

    private static ChaveDeduplicacao Hash(string identificacao, int ordinal)
    {
        string texto = $"{identificacao}{Separador}{ordinal.ToString(CultureInfo.InvariantCulture)}";
        return new ChaveDeduplicacao(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(texto))));
    }

    private static string Identificacao(Banco banco, OrigemTransacao origem, IdentidadeLancamento identidade)
    {
        (string tipoOrigem, Guid idOrigem) = origem.IdConta is IdConta idConta
            ? ("conta", (Guid)idConta)
            : ("cartao", (Guid)origem.IdCartao!.Value);

        IEnumerable<string> campos =
        [
            Versao,
            banco,
            tipoOrigem,
            idOrigem.ToString("D"),
            identidade.EhUnica ? "unica" : "composta",
            .. identidade.Campos,
        ];

        return string.Join(Separador, campos.Select(Escapar));
    }

    // Um separador dentro de um campo não pode deslocar os campos seguintes.
    private static string Escapar(string campo) => campo.Replace("\\", "\\\\").Replace("|", "\\|");
}
