using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Importacoes;

[ValueObject]
public readonly partial struct IdImportacao
{
    private static Validation Validate(Guid input) => Id.Validar(input);
}

/// <summary>
/// O registro de um arquivo importado: de qual origem, em qual layout, que período cobria, quantos
/// lançamentos trazia e, na fatura, quando ela vence. É por ele que a importação seguinte descobre se ficou um buraco entre as duas.
/// </summary>
/// <remarks>
/// Cada transação guarda o <see cref="IdImportacao"/> da importação que a criou. Quantas eram novas não fica
/// aqui: são as transações ligadas a ela.
/// </remarks>
public sealed class ImportacaoArquivo : AggregateRoot<IdImportacao>, IPertenceAoUsuario
{
    public IdUsuario IdUsuario { get; private set; }
    public OrigemTransacao Origem { get; private set; } = null!;
    public LayoutArquivoImportacao Layout { get; private set; } = null!;
    public PeriodoArquivo Periodo { get; private set; } = null!;
    public int Lidas { get; private set; }
    public DateTimeOffset ImportadaEm { get; private set; }

    /// <summary>O vencimento da fatura do arquivo; nulo no extrato de uma conta.</summary>
    public VencimentoFatura? VencimentoFatura { get; private set; }

    /// <summary>O mês em que vence a fatura do arquivo, o do <see cref="VencimentoFatura"/>; nulo no extrato de uma conta.</summary>
    public MesReferencia? MesVencimentoFatura => VencimentoFatura?.Mes;

    /// <summary>
    /// Se esta importação é de um cartão da <paramref name="fatura"/>, com vencimento no mesmo mês do dela, mas em outra
    /// data, pelo <see cref="Cartoes.VencimentoFatura.EhOutraDataNoMesmoMes"/>. O extrato de uma conta nunca é.
    /// </summary>
    public bool TemOutroVencimentoNoMesmoMes(FaturaCartao fatura)
    {
        return Origem.IdCartao is IdCartao idCartao
            && fatura.Contem(idCartao)
            && VencimentoFatura is not null
            && VencimentoFatura.EhOutraDataNoMesmoMes(fatura.Vencimento);
    }

    private ImportacaoArquivo()
    {
    }

    private ImportacaoArquivo(
        IdImportacao id,
        IdUsuario idUsuario,
        OrigemTransacao origem,
        LayoutArquivoImportacao layout,
        PeriodoArquivo periodo,
        int lidas,
        DateTimeOffset importadaEm,
        VencimentoFatura? vencimentoFatura)
            : base(id)
    {
        IdUsuario = idUsuario;
        Origem = origem;
        Layout = layout;
        Periodo = periodo;
        Lidas = lidas;
        ImportadaEm = importadaEm;
        VencimentoFatura = vencimentoFatura;
    }

    /// <summary>A importação do extrato de uma conta, que não tem vencimento.</summary>
    /// <param name="lidas">Quantos lançamentos o arquivo trazia.</param>
    /// <remarks>
    /// Interno pois o <paramref name="idUsuario"/> precisa ser o dono da conta da <paramref name="origem"/>, e só a
    /// <see cref="Services.ImportacaoArquivoFactory"/>, que recebe a conta, garante isso.
    /// </remarks>
    internal static ImportacaoArquivo RegistrarExtrato(
        IdUsuario idUsuario,
        OrigemTransacao origem,
        LayoutArquivoImportacao layout,
        PeriodoArquivo periodo,
        int lidas,
        DateTimeOffset importadaEm)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(lidas);
        return new(IdImportacao.FromNewVersion7Guid(), idUsuario, origem, layout, periodo, lidas, importadaEm, vencimentoFatura: null);
    }

    /// <summary>A importação da fatura de um cartão, com a data em que ela vence.</summary>
    /// <param name="lidas">Quantos lançamentos o arquivo trazia.</param>
    /// <remarks>
    /// Interno pois o <paramref name="idUsuario"/> precisa ser o dono do cartão da <paramref name="origem"/>, e só a
    /// <see cref="Services.ImportacaoArquivoFactory"/>, que recebe o cartão, garante isso.
    /// </remarks>
    internal static ImportacaoArquivo RegistrarFatura(
        IdUsuario idUsuario,
        OrigemTransacao origem,
        LayoutArquivoImportacao layout,
        PeriodoArquivo periodo,
        VencimentoFatura vencimento,
        int lidas,
        DateTimeOffset importadaEm)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(lidas);
        return new(IdImportacao.FromNewVersion7Guid(), idUsuario, origem, layout, periodo, lidas, importadaEm, vencimento);
    }

    /// <summary>
    /// Os dias entre o fim da <paramref name="ultima"/> importação da origem e o início desta, que nenhum
    /// arquivo cobriu. Nada quando é a primeira importação ou quando os períodos se tocam ou se sobrepõem.
    /// </summary>
    /// <param name="ultima">A importação mais recente da mesma origem, antes desta.</param>
    public Maybe<PeriodoArquivo> PeriodoNaoImportado(Maybe<ImportacaoArquivo> ultima)
    {
        if (ultima.HasNoValue)
            return Maybe.None;

        if (ultima.Value.Origem != Origem)
            throw new ArgumentException("A importação anterior é de outra origem.", nameof(ultima));

        // O período entre as duas só existe quando sobra pelo menos um dia: com os períodos se tocando ou se
        // sobrepondo, o início fica depois do fim, o Criar falha, e não há período não importado.
        return PeriodoArquivo
            .Criar(ultima.Value.Periodo.Fim.AddDays(1), Periodo.Inicio.AddDays(-1))
            .AsMaybe();
    }
}
