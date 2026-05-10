namespace Languag.io.Application.Common;

public static class UserTimeZones
{
    public const string DefaultTimeZoneId = "UTC";

    public static string NormalizeOrDefault(string? timeZoneId)
    {
        return string.IsNullOrWhiteSpace(timeZoneId)
            ? DefaultTimeZoneId
            : timeZoneId.Trim();
    }

    public static bool IsValid(string timeZoneId)
    {
        return TryFind(timeZoneId, out _);
    }

    public static TimeZoneInfo FindOrDefault(string? timeZoneId)
    {
        return TryFind(NormalizeOrDefault(timeZoneId), out var timeZone)
            ? timeZone
            : TimeZoneInfo.Utc;
    }

    public static DateTime GetLocalDate(DateTime utc, TimeZoneInfo timeZone)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(EnsureUtc(utc), timeZone).Date;
    }

    public static DateTime LocalDateStartToUtc(DateTime localDate, TimeZoneInfo timeZone)
    {
        return TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(localDate.Date, DateTimeKind.Unspecified),
            timeZone);
    }

    private static bool TryFind(string? timeZoneId, out TimeZoneInfo timeZone)
    {
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(NormalizeOrDefault(timeZoneId));
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            timeZone = TimeZoneInfo.Utc;
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            timeZone = TimeZoneInfo.Utc;
            return false;
        }
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
