namespace ControleFinanceiro.Domain.Contas;

/// <summary>Enxerga só as contas do usuário atual.</summary>
public interface IContaRepository
{
    Task<IReadOnlyList<ContaBancaria>> ListarAsync(CancellationToken cancellationToken);

    Task AdicionarAsync(ContaBancaria conta, CancellationToken cancellationToken);
}
