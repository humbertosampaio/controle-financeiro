namespace ControleFinanceiro.Domain.Shared;

/// <summary>
/// Um fato que a raiz de um agregado registra ao mudar de estado, para que um tratador altere outro agregado depois, na
/// própria transação (padrões de domínio, §6).
/// </summary>
public interface IDomainEvent
{
}
