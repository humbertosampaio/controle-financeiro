namespace ControleFinanceiro.Domain.Shared;

public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
    where TId : IComparable, IComparable<TId>
{
    private readonly List<IDomainEvent> _eventos = [];

    public IReadOnlyList<IDomainEvent> Eventos => _eventos;

    protected AggregateRoot() : base()
    {
    }

    protected AggregateRoot(TId id) : base(id)
    {
    }

    /// <summary>Registra um fato desta raiz, gravado no commit que a grava.</summary>
    protected void RegistrarEvento(IDomainEvent evento)
    {
        _eventos.Add(evento);
    }

    void IAggregateRoot.LimparEventos()
    {
        _eventos.Clear();
    }
}
