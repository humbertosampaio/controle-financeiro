using ControleFinanceiro.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.Infrastructure.Persistence.Outbox;

/// <summary>
/// Grava as mensagens do outbox. Sem porta no domínio, porque a mensagem não é agregado: só a Persistence a usa.
/// </summary>
internal sealed class MensagemOutboxRepository
{
    private readonly DbSet<MensagemOutbox> _dbSet;
    private readonly TimeProvider _timeProvider;

    public MensagemOutboxRepository(ControleFinanceiroDbContext dbContext, TimeProvider timeProvider)
    {
        _dbSet = dbContext.MensagensOutbox;
        _timeProvider = timeProvider;
    }

    /// <summary>Uma mensagem pendente por evento, gravada no próximo <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>.</summary>
    public void Adicionar(IEnumerable<IDomainEvent> eventos)
    {
        DateTimeOffset registradaEm = _timeProvider.GetUtcNow();
        MensagemOutbox[] mensagens = eventos
            .Select(evento => MensagemOutbox.Registrar(
                DomainEventSerializer.GetTypeName(evento),
                DomainEventSerializer.Serialize(evento),
                registradaEm))
            .ToArray();

        _dbSet.AddRange(mensagens);
    }
}
