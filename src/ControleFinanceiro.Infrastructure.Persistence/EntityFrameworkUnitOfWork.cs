using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Infrastructure.Persistence.Outbox;

namespace ControleFinanceiro.Infrastructure.Persistence;

internal sealed class EntityFrameworkUnitOfWork : IUnitOfWork
{
    private readonly ControleFinanceiroDbContext _controleFinanceiroDbContext;
    private readonly MensagemOutboxRepository _mensagemOutboxRepository;

    public EntityFrameworkUnitOfWork(ControleFinanceiroDbContext dbContext, MensagemOutboxRepository mensagemOutboxRepository)
    {
        _controleFinanceiroDbContext = dbContext;
        _mensagemOutboxRepository = mensagemOutboxRepository;
    }

    /// <summary>
    /// Grava os agregados e, na mesma transação, uma mensagem do outbox por evento que as raízes registraram. Os eventos
    /// são esquecidos antes do <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync(CancellationToken)"/>,
    /// para um commit repetido não gravar a mesma mensagem de novo.
    /// </summary>
    public async Task<int> CommitAsync(CancellationToken cancellationToken)
    {
        IAggregateRoot[] raizes = _controleFinanceiroDbContext.ChangeTracker
            .Entries<IAggregateRoot>()
            .Select(entrada => entrada.Entity)
            .ToArray();

        _mensagemOutboxRepository.Adicionar(raizes.SelectMany(raiz => raiz.Eventos).ToArray());

        foreach (IAggregateRoot raiz in raizes)
            raiz.LimparEventos();

        return await _controleFinanceiroDbContext.SaveChangesAsync(cancellationToken);
    }
}
