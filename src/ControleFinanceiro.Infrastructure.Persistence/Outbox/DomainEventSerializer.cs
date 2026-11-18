using ControleFinanceiro.Domain.Shared;
using System.Reflection;
using System.Text.Json;

namespace ControleFinanceiro.Infrastructure.Persistence.Outbox;

/// <summary>
/// Leva o evento de domínio ao conteúdo da <see cref="MensagemOutbox"/>. O tipo é o nome estável que o evento declara no
/// <see cref="DomainEventNameAttribute"/>, e não o da classe, e os ids Vogen vão como o <see cref="Guid"/>, pelo
/// conversor que o Vogen gera para o System.Text.Json.
/// </summary>
internal static class DomainEventSerializer
{
    /// <exception cref="InvalidOperationException">O evento não declara o <see cref="DomainEventNameAttribute"/>.</exception>
    public static string GetTypeName(IDomainEvent evento)
    {
        Type tipo = evento.GetType();

        return tipo.GetCustomAttribute<DomainEventNameAttribute>()?.Name
            ?? throw new InvalidOperationException($"O evento {tipo.Name} não declara o {nameof(DomainEventNameAttribute)}.");
    }

    public static string Serialize(IDomainEvent evento) =>
        JsonSerializer.Serialize(evento, evento.GetType());
}
