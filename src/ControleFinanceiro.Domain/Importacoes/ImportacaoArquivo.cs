using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Importacoes;

[ValueObject]
public readonly partial struct IdImportacao
{
    private static Validation Validate(Guid input) => Id.Validar(input);
}

/// <summary>
/// O registro de um arquivo importado: em qual layout, que período cobria e quantos lançamentos trazia. O extrato de uma
/// conta é a <see cref="ImportacaoExtrato"/>, e a fatura de um cartão, com o vencimento, a <see cref="ImportacaoFatura"/>.
/// </summary>
/// <remarks>
/// Cada transação guarda o <see cref="IdImportacao"/> da importação que a criou. Quantas eram novas não fica
/// aqui: são as transações ligadas a ela.
/// </remarks>
public abstract class ImportacaoArquivo : AggregateRoot<IdImportacao>, IPertenceAoUsuario
{
    public IdUsuario IdUsuario { get; private set; }
    public LayoutArquivoImportacao Layout { get; private set; } = null!;
    public Periodo Periodo { get; private set; } = null!;
    public int Lidas { get; private set; }
    public DateTimeOffset ImportadaEm { get; private set; }

    protected ImportacaoArquivo()
    {
    }

    /// <param name="lidas">Quantos lançamentos o arquivo trazia.</param>
    protected ImportacaoArquivo(
        IdImportacao id,
        IdUsuario idUsuario,
        LayoutArquivoImportacao layout,
        Periodo periodo,
        int lidas,
        DateTimeOffset importadaEm)
            : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(lidas);

        IdUsuario = idUsuario;
        Layout = layout;
        Periodo = periodo;
        Lidas = lidas;
        ImportadaEm = importadaEm;
    }
}
