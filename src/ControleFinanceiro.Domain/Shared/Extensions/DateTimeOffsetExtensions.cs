namespace System;

public static class DateTimeOffsetExtensions
{
    extension(DateTimeOffset dateTimeOffset)
    {
        public DateOnly ToDateOnly() => DateOnly.FromDateTime(dateTimeOffset.LocalDateTime);
    }
}
