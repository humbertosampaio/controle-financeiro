namespace System;

public static class DateOnlyExtensions
{
    extension(DateOnly dateOnly)
    {
        public DateTimeOffset ToLocalDateTimeOffset() => dateOnly.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local);
    }
}
