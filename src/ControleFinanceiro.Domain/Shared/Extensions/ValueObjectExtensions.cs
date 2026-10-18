namespace ControleFinanceiro.Domain.Shared.Extensions;

public static class ValueObjectExtensions
{
    extension<TPrimitiveId>(TPrimitiveId value)
        where TPrimitiveId : IComparable, IComparable<TPrimitiveId>
    {
        public TTo ToId<TTo>() where TTo : IVogen<TTo, TPrimitiveId> =>
            TTo.From(value);
    }

    extension(Guid value)
    {
        public TTo ToId<TTo>() where TTo : IVogen<TTo, Guid> =>
            value.ToId<Guid, TTo>();
    }
}
