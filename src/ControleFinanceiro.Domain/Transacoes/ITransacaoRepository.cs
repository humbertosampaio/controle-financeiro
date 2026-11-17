using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Transacoes;

/// <summary>
/// Enxerga só as transações do usuário atual, das duas origens. Quem trabalha com uma origem só usa o
/// <see cref="ITransacaoContaRepository"/> ou o <see cref="ITransacaoCartaoRepository"/>.
/// </summary>
public interface ITransacaoRepository
{
    /// <summary>A transação do id que chega de fora, de qualquer origem: quem a recebe resolve o tipo.</summary>
    Task<Maybe<Transacao>> ObterAsync(IdTransacao idTransacao, CancellationToken cancellationToken);

    /// <summary>As transações de todas as origens com a data dentro do mês, em ordem de data.</summary>
    Task<IReadOnlyList<Transacao>> ListarDoMesAsync(MesReferencia mesReferencia, CancellationToken cancellationToken);
}
