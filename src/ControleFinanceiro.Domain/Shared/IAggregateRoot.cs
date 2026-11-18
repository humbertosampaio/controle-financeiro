namespace ControleFinanceiro.Domain.Shared;

/// <summary>
/// A raiz de um agregado, vista sem o tipo do id: é por ela que quem grava a raiz lê os eventos que ela registrou.
/// </summary>
public interface IAggregateRoot
{
    /// <summary>Os eventos registrados desde o último commit, na ordem em que aconteceram.</summary>
    IReadOnlyList<IDomainEvent> Eventos { get; }

    /// <summary>Esquece os eventos registrados, depois de eles entrarem no commit.</summary>
    void LimparEventos();
}
