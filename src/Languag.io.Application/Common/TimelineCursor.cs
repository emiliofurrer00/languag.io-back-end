using System.Globalization;
using System.Text;

namespace Languag.io.Application.Common;

public readonly record struct TimelineCursor(DateTime CreatedAtUtc, Guid Id)
{
    public string Encode()
    {
        var payload = $"{CreatedAtUtc.ToUniversalTime().Ticks.ToString(CultureInfo.InvariantCulture)}|{Id:D}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
    }

    public static bool TryDecode(string? cursor, out TimelineCursor parsedCursor)
    {
        parsedCursor = default;

        if (string.IsNullOrWhiteSpace(cursor))
        {
            return false;
        }

        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = raw.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 2
                || !long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks)
                || !Guid.TryParse(parts[1], out var id))
            {
                return false;
            }

            parsedCursor = new TimelineCursor(new DateTime(ticks, DateTimeKind.Utc), id);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
