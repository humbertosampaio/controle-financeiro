namespace ControleFinanceiro.Application.Abstractions;

public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken cancellationToken);
}
