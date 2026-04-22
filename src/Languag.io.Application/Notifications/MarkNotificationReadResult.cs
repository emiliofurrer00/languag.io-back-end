namespace Languag.io.Application.Notifications;

public enum MarkNotificationReadStatus
{
    Success = 1,
    NotFound = 2,
    Forbidden = 3
}

public sealed record MarkNotificationReadResult(
    MarkNotificationReadStatus Status);
