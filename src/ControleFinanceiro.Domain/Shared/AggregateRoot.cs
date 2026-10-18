namespace ControleFinanceiro.Domain.Shared;

public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : IComparable, IComparable<TId>
{
    protected AggregateRoot() : base()
    {
    }

    protected AggregateRoot(TId id) : base(id)
    {
    }
}
