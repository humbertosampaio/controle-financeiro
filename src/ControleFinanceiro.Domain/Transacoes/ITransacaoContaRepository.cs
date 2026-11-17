using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Transacoes;

/// <summary>Enxerga só as transações da conta do usuário atual.</summary>
public interface ITransacaoContaRepository
{
    /// <summary>
    /// Adiciona só as transações cuja chave de deduplicação ainda não está no banco de dados, em nenhuma das origens.
    /// </summary>
    /// <returns>As transações adicionadas: as mesmas instâncias recebidas, na mesma ordem.</returns>
    Task<IReadOnlyList<TransacaoConta>> AdicionarNovasAsync(IEnumerable<TransacaoConta> transacoes, CancellationToken cancellationToken);

    /// <summary>As transações da conta gravadas no banco de dados com a data dentro do período.</summary>
    Task<IReadOnlyList<TransacaoConta>> ListarNoPeriodoAsync(IdConta idConta, Periodo periodo, CancellationToken cancellationToken);

    /// <summary>As transações de todas as contas com a data dentro do mês.</summary>
    Task<IReadOnlyList<TransacaoConta>> ListarDoMesAsync(MesReferencia mesReferencia, CancellationToken cancellationToken);
}
