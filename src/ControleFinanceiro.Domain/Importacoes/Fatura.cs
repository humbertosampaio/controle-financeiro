using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Domain.Importacoes;

/// <summary>
/// A fatura fechada de um cartão: o banco e os lançamentos na ordem do arquivo, cada um com o final do cartão que
/// fez a compra. Um arquivo traz vários finais (o cartão físico e o virtual), e cada final é um cartão. Do cartão,
/// só o final: o número completo e o titular não entram nela.
/// </summary>
/// <remarks>
/// A fatura não declara período nem saldo. O período de cada cartão vai da menor à maior data de compra, e a
/// parcela de uma compra antiga traz a data da compra original, então ele pode cobrir vários meses.
/// </remarks>
public sealed class Fatura : ValueObject
{
    private readonly List<LancamentoFatura> _lancamentos;
    private readonly List<SufixoCartao> _sufixos;

    public Banco Banco { get; }
    public IReadOnlyList<LancamentoFatura> Lancamentos => _lancamentos;

    /// <summary>Os finais distintos, na ordem em que aparecem pela primeira vez no arquivo.</summary>
    public IReadOnlyList<SufixoCartao> Sufixos => _sufixos;

    private Fatura(Banco banco, List<LancamentoFatura> lancamentos)
    {
        Banco = banco;
        _lancamentos = lancamentos;
        _sufixos = lancamentos
            .Select(l => l.SufixoCartao)
            .Distinct()
            .ToList();
    }

    /// <summary>Falha quando a fatura não traz nenhum lançamento.</summary>
    public static Result<Fatura> Criar(Banco banco, IEnumerable<LancamentoFatura> lancamentos)
    {
        List<LancamentoFatura> lancamentosFatura = lancamentos.ToList();

        return Result.SuccessIf(
            lancamentosFatura.Count > 0,
            value: new Fatura(banco, lancamentosFatura),
            "A fatura não traz lançamentos.");
    }

    /// <summary>Os lançamentos do cartão, na ordem do arquivo, como a <see cref="Services.LoteTransacoesFactory"/> os recebe.</summary>
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

    /// <summary>Da menor à maior data de compra do cartão.</summary>
    /// <exception cref="ArgumentException">Quando o final não está na fatura.</exception>
    public PeriodoArquivo PeriodoCartao(SufixoCartao sufixo)
    {
        IReadOnlyList<Lancamento> lancamentos = LancamentosCartao(sufixo);
        return (PeriodoArquivo)(lancamentos.Min(l => l.Data), lancamentos.Max(l => l.Data));
    }

    private void GarantirSufixo(SufixoCartao sufixo)
    {
        if (!_sufixos.Contains(sufixo))
            throw new ArgumentException("O final não está na fatura.", nameof(sufixo));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Banco;

        foreach (LancamentoFatura lancamento in _lancamentos)
            yield return lancamento;
    }
}
