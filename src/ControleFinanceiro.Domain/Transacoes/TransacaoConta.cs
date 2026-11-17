using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Transacoes;

/// <summary>Uma transação vinda do extrato de uma conta, como a saída que paga uma despesa ou a fatura do cartão.</summary>
public sealed class TransacaoConta : Transacao
{
    public IdConta IdConta { get; private set; }

    /// <summary>A contraparte na saída, pela regra do <see cref="LancamentoConta.TextoBeneficiario"/>.</summary>
    public override string? TextoBeneficiario => LancamentoConta.TextoBeneficiarioDe(EhSaida, Contraparte);

    private TransacaoConta()
    {
    }

    private TransacaoConta(
        IdTransacao id,
        IdUsuario idUsuario,
        IdImportacao idImportacao,
        IdConta idConta,
        LancamentoConta lancamento,
        ChaveDeduplicacao chave)
            : base(id, idUsuario, idImportacao, lancamento, chave)
    {
        IdConta = idConta;
    }

    /// <remarks>
    /// Interno pois a <paramref name="chave"/> precisa vir de <see cref="ChaveDeduplicacao.CalcularLote(Shared.ValueObjects.Banco, Contas.IdConta, IEnumerable{IdentidadeLancamento})"/>, com a mesma conta e o lote do
    /// lançamento, o <paramref name="idUsuario"/> precisa ser o dono da conta e a importação precisa ser da mesma conta, e só a
    /// <see cref="Services.LoteTransacoesContaFactory"/> garante isso.
    /// </remarks>
    internal static TransacaoConta Criar(
        IdUsuario idUsuario,
        IdImportacao idImportacao,
        IdConta idConta,
        LancamentoConta lancamento,
        ChaveDeduplicacao chave)
    {
        return new TransacaoConta(IdTransacao.FromNewVersion7Guid(), idUsuario, idImportacao, idConta, lancamento, chave);
    }
}
