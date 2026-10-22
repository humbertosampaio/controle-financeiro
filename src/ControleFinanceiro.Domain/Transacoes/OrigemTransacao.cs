using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Contas;

namespace ControleFinanceiro.Domain.Transacoes;

/// <summary>
/// De onde veio a transação: o extrato de uma conta ou a fatura de um cartão, nunca os dois.
/// </summary>
public sealed class OrigemTransacao : ValueObject
{
    public IdConta? IdConta { get; }
    public IdCartao? IdCartao { get; }

    private OrigemTransacao(IdConta? idConta, IdCartao? idCartao)
    {
        IdConta = idConta;
        IdCartao = idCartao;
    }

    public static OrigemTransacao DaConta(IdConta idConta) => new(idConta, idCartao: null);

    public static OrigemTransacao DoCartao(IdCartao idCartao) => new(idConta: null, idCartao);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return IdConta;
        yield return IdCartao;
    }
}
