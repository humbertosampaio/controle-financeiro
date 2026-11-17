using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Domain.Importacoes;

/// <summary>
/// A fatura fechada de um cartão: o banco, a data de vencimento e os lançamentos na ordem do arquivo, cada um com o
/// final do cartão que fez a compra. Um arquivo traz vários finais (o cartão físico e o virtual), e cada final é um
/// cartão. Do cartão, só o final: o número completo e o titular não entram nela.
/// </summary>
/// <remarks>
/// O vencimento vem do arquivo, quando o layout o traz, ou do usuário, quando não traz. A fatura não declara período
/// nem saldo. O período de cada cartão, e o da fatura inteira, vai da menor à maior data de compra, e a parcela de uma
/// compra antiga traz a data da compra original, então ele pode cobrir vários meses.
/// </remarks>
public sealed class Fatura : ValueObject
{
    /// <summary>
    /// Quantos meses, no máximo, a parcela de uma compra que a fatura traz vence depois do mês da compra, contada a partir
    /// da primeira: nenhum ciclo cadastrável põe a primeira fatura de uma compra mais longe que isso.
    /// </summary>
    public const int MaximoMesesAteVencimento = 2;

    private readonly List<LancamentoFatura> _lancamentos;
    private readonly List<SufixoCartao> _sufixos;

    public Banco Banco { get; }

    /// <summary>O vencimento da fatura, o mesmo para todos os finais.</summary>
    public VencimentoFatura Vencimento { get; }

    public IReadOnlyList<LancamentoFatura> Lancamentos => _lancamentos;

    /// <summary>Os finais distintos, na ordem em que aparecem pela primeira vez no arquivo.</summary>
    public IReadOnlyList<SufixoCartao> Sufixos => _sufixos;

    private Fatura(Banco banco, VencimentoFatura vencimento, List<LancamentoFatura> lancamentos)
    {
        Banco = banco;
        Vencimento = vencimento;
        _lancamentos = lancamentos;
        _sufixos = lancamentos
            .Select(l => l.SufixoCartao)
            .Distinct()
            .ToList();
    }

    /// <summary>Falha quando a fatura não traz nenhum lançamento.</summary>
    public static Result<Fatura> Criar(Banco banco, VencimentoFatura vencimento, IEnumerable<LancamentoFatura> lancamentos)
    {
        List<LancamentoFatura> lancamentosFatura = lancamentos.ToList();

        return Result.SuccessIf(
            lancamentosFatura.Count > 0,
            value: new Fatura(banco, vencimento, lancamentosFatura),
            "A fatura não traz lançamentos.");
    }

    /// <summary>Os lançamentos do cartão, na ordem do arquivo, como a <see cref="Services.LoteTransacoesCartaoFactory"/> os recebe.</summary>
    /// <exception cref="ArgumentException">Quando o final não está na fatura.</exception>
    public IReadOnlyList<Lancamento> LancamentosCartao(SufixoCartao sufixo)
    {
        GarantirSufixo(sufixo);

        return _lancamentos
            .Where(l => l.SufixoCartao == sufixo)
            .Select(l => l.Lancamento)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Toda fatura traz o pagamento da anterior, num dos cartões. Falha quando nenhum lançamento é o pagamento
    /// (<see cref="Lancamento.EhPagamentoFatura"/>): na primeira fatura do cartão e na fatura anterior zerada isso é
    /// esperado, e nas demais costuma ser o layout do banco que mudou. Não impede a importação. A falha é um texto fixo,
    /// sem dado do arquivo.
    /// </summary>
    public Result ConferirPagamentoAnterior()
    {
        return Result.SuccessIf(
            _lancamentos.Any(l => l.Lancamento.EhPagamentoFatura),
            "A fatura não traz o pagamento da fatura anterior: se não é a primeira fatura do cartão, confira se o layout do banco mudou.");
    }

    /// <summary>
    /// Confere o vencimento com as compras, as saídas da fatura: a entrada, como o pagamento da fatura anterior e o
    /// estorno, não tem o que conferir. Falha quando uma compra à vista é do dia do vencimento ou depois, ou quando,
    /// para a parcela <c>n</c> de uma compra (a compra à vista conta como a parcela 1), o mês do vencimento fica antes do
    /// mês da compra avançado <c>n − 1</c> meses ou mais de <see cref="MaximoMesesAteVencimento"/> meses depois dele.
    /// </summary>
    public UnitResult<Erro> ConferirVencimento()
    {
        MesReferencia mesVencimento = Vencimento.Mes;
        bool confere = Compras.All(compra => CompraAVistaAntesVencimento(compra) && ParcelaVenceNestaFatura(compra, mesVencimento));

        return UnitResult.SuccessIf<Erro>(confere, ErroVencimentoIncompativelComFatura.Instancia);
    }

    /// <summary>As saídas da fatura. As entradas, como o pagamento da fatura anterior e o estorno, ficam de fora.</summary>
    private IEnumerable<Lancamento> Compras => _lancamentos.Select(l => l.Lancamento).Where(l => l.EhSaida);

    /// <summary>
    /// A fatura fecha antes de vencer, então a compra à vista dela é anterior ao vencimento. A parcela traz a data da
    /// compra original, que é antiga, e fica de fora.
    /// </summary>
    /// <param name="compra">Uma saída da fatura.</param>
    private bool CompraAVistaAntesVencimento(Lancamento compra) => !compra.EhAVista || compra.Data < Vencimento;

    /// <summary>
    /// O mês do vencimento está entre o primeiro mês em que a compra pode vencer e
    /// <see cref="MaximoMesesAteVencimento"/> meses depois dele.
    /// </summary>
    /// <param name="compra">Uma saída da fatura.</param>
    /// <param name="mesVencimento">O mês do <see cref="Vencimento"/>.</param>
    private static bool ParcelaVenceNestaFatura(Lancamento compra, MesReferencia mesVencimento)
    {
        MesReferencia primeiroMes = compra.PrimeiroMesVencimentoPossivel;
        MesReferencia ultimoMes = primeiroMes.Posterior(MaximoMesesAteVencimento);

        return mesVencimento >= primeiroMes && mesVencimento <= ultimoMes;
    }

    /// <summary>Da menor à maior data de compra de todos os cartões.</summary>
    public Periodo Periodo => PeriodoDe(_lancamentos.Select(l => l.Lancamento).ToList());

    /// <summary>Da menor à maior data de compra do cartão.</summary>
    /// <exception cref="ArgumentException">Quando o final não está na fatura.</exception>
    public Periodo PeriodoCartao(SufixoCartao sufixo) => PeriodoDe(LancamentosCartao(sufixo));

    /// <param name="lancamentos">Pelo menos um.</param>
    private static Periodo PeriodoDe(IReadOnlyList<Lancamento> lancamentos) =>
        new Periodo(lancamentos.Min(l => l.Data), lancamentos.Max(l => l.Data));

    private void GarantirSufixo(SufixoCartao sufixo)
    {
        if (!_sufixos.Contains(sufixo))
            throw new ArgumentException("O final não está na fatura.", nameof(sufixo));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Banco;
        yield return Vencimento;

        foreach (LancamentoFatura lancamento in _lancamentos)
            yield return lancamento;
    }
}
