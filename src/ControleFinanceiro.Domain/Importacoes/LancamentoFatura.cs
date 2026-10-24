using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Domain.Importacoes;

/// <summary>
/// Um lançamento da fatura com o final do cartão que fez a compra. O final só serve para a <see cref="Fatura"/>
/// separar os lançamentos por cartão e não é gravado na transação, que guarda o id do cartão.
/// </summary>
public sealed class LancamentoFatura : ValueObject
{
    public Lancamento Lancamento { get; }
    public SufixoCartao SufixoCartao { get; }

    private LancamentoFatura(Lancamento lancamento, SufixoCartao sufixoCartao)
    {
        Lancamento = lancamento;
        SufixoCartao = sufixoCartao;
    }

    public static LancamentoFatura Criar(Lancamento lancamento, SufixoCartao sufixoCartao) => new(lancamento, sufixoCartao);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Lancamento;
        yield return SufixoCartao;
    }
}
